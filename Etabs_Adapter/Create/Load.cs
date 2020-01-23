/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System.Collections.Generic;
using System.Linq;
using System;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.Engine.ETABS;
using BH.Engine.Common;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Base;

#if Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/

        public bool CreateObject(Loadcase loadcase)
        {
            eLoadPatternType patternType = loadcase.Nature.ToCSI();
            double selfWeight = 0;

            int ret = m_model.LoadPatterns.Add(loadcase.Name, patternType, selfWeight, true);
            loadcase.CustomData[AdapterId] = loadcase.Name;

            return true;
        }

        /***************************************************/

        private bool CreateObject(MassSource massSource)
        {
            bool includeElements = massSource.ElementSelfMass;
            bool includeAddMass = massSource.AdditionalMass;
            bool includeLoads = massSource.FactoredAdditionalCases.Count > 0;

            int count = massSource.FactoredAdditionalCases.Count;
            string[] cases = new string[count];
            double[] factors = new double[count];

            for (int i = 0; i < count; i++)
            {
                cases[i] = massSource.FactoredAdditionalCases[i].Item1.Name;
                factors[i] = massSource.FactoredAdditionalCases[i].Item2;
            }

            if (m_model.PropMaterial.SetMassSource_1(ref includeElements, ref includeAddMass, ref includeLoads, count, ref cases, ref factors) == 0) { }
            else
            {
                CreateElementError("mass source", massSource.Name);
            }

            return true;
        }

        /***************************************************/

        private bool CreateObject(ModalCase modalCase)
        {
            m_model.Analyze.SetRunCaseFlag("Modal", true);

            return true;
        }

        /***************************************************/

        private bool CreateObject(LoadCombination loadCombination)
        {
            if (m_model.RespCombo.Add(loadCombination.Name, 0) == 0) //0=case, 1=combo 
            {
                foreach (var factorCase in loadCombination.LoadCases)
                {
                    double factor = factorCase.Item1;
                    Type lcType = factorCase.Item2.GetType();
                    string lcName = factorCase.Item2.Name;// factorCase.Item2.Name;// Number.ToString();
                    eCNameType cTypeName = eCNameType.LoadCase;

                    if (lcType == typeof(Loadcase))
                        cTypeName = eCNameType.LoadCase;
                    else if (lcType == typeof(LoadCombination))
                        cTypeName = eCNameType.LoadCombo;

                    m_model.RespCombo.SetCaseList(loadCombination.Name, ref cTypeName, lcName, factor);
                }
                loadCombination.CustomData[AdapterId] = loadCombination.Name;
            }
            else
            {
                CreateElementError(loadCombination.GetType().ToString(), loadCombination.Name);
            }

            return true;
        }

        /***************************************************/

        private bool CreateObject(ILoad bhLoad)
        {
            SetLoad(bhLoad as dynamic, this.EtabsConfig.ReplaceLoads);

            return true;
        }
        
        /***************************************************/

        public void SetLoad(PointLoad pointLoad, bool replace)
        {
            double[] pfValues = new double[] { pointLoad.Force.X, pointLoad.Force.Y, pointLoad.Force.Z, pointLoad.Moment.X, pointLoad.Moment.Y, pointLoad.Moment.Z };
            int ret = 0;
            foreach (Node node in pointLoad.Objects.Elements)
            {
                string caseName = pointLoad.Loadcase.CustomData[AdapterId].ToString();
                string nodeName = node.CustomData[AdapterId].ToString();
                ret = m_model.PointObj.SetLoadForce(nodeName, caseName, ref pfValues, replace);
            }
        }

        /***************************************************/

        public void SetLoad(BarUniformlyDistributedLoad barUniformLoad, bool replace)
        {

            foreach (Bar bar in barUniformLoad.Objects.Elements)
            {
                bool stepReplace = replace;

                string caseName = barUniformLoad.Loadcase.CustomData[AdapterId].ToString();
                string barName = bar.CustomData[AdapterId].ToString();

                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barUniformLoad.Force.X : direction == 2 ? barUniformLoad.Force.Y : barUniformLoad.Force.Z; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadDistributed(barName, caseName, 1, direction + 3, 0, 1, val, val, "Global", true, stepReplace);
                    }

                    stepReplace = false;
                }
                //moments ? does not exist in old toolkit either! 
            }
        }

        /***************************************************/

        public void SetLoad(AreaUniformlyDistributedLoad areaUniformLoad, bool replace)
        {
            int ret = 0;
            string caseName = areaUniformLoad.Loadcase.CustomData[AdapterId].ToString();
            foreach (IAreaElement area in areaUniformLoad.Objects.Elements)
            {
                for (int direction = 1; direction <= 3; direction++)
                {
                    double val = direction == 1 ? areaUniformLoad.Pressure.X : direction == 2 ? areaUniformLoad.Pressure.Y : areaUniformLoad.Pressure.Z;
                    if (val != 0)
                    {
                        //NOTE: Replace=false has been set to allow setting x,y,z-load directions !!! this should be user controled and allowed as default
                        ret = m_model.AreaObj.SetLoadUniform(area.CustomData[AdapterId].ToString(), caseName, val, direction + 3, replace);
                    }
                }
            }
        }

        /***************************************************/

        public void SetLoad(BarVaryingDistributedLoad barLoad, bool replace)
        {
            int ret = 0;

            foreach (Bar bar in barLoad.Objects.Elements)
            {
                {
                    double val1 = barLoad.ForceA.Z; //note: etabs acts different then stated in API documentstion
                    double val2 = barLoad.ForceB.Z;
                    double dist1 = barLoad.DistanceFromA;
                    double dist2 = bar.Length() - barLoad.DistanceFromB;
                    string caseName = barLoad.Loadcase.CustomData[AdapterId].ToString();
                    string nodeName = bar.CustomData[AdapterId].ToString();
                    int direction = 6; // we're doing this for Z axis only right now.
                    ret = m_model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterId].ToString(), caseName, 1, direction, dist1, dist2, val1, val2, "Global", false, replace);
                }
            }
        }

        /***************************************************/

        public void SetLoad(GravityLoad gravityLoad, bool replace)
        {
            double selfWeightMultiplier = 0;

            string caseName = gravityLoad.Loadcase.CustomData[AdapterId].ToString();

            m_model.LoadPatterns.GetSelfWTMultiplier(caseName, ref selfWeightMultiplier);

            if (selfWeightMultiplier != 0)
                BH.Engine.Reflection.Compute.RecordWarning($"Loadcase {gravityLoad.Loadcase.Name} allready had a selfweight multiplier which will get overridden. Previous value: {selfWeightMultiplier}, new value: {-gravityLoad.GravityDirection.Z}");

            m_model.LoadPatterns.SetSelfWTMultiplier(caseName, -gravityLoad.GravityDirection.Z);

            if (gravityLoad.GravityDirection.X != 0 || gravityLoad.GravityDirection.Y != 0)
                Engine.Reflection.Compute.RecordError("ETABS can only handle gravity loads in global z direction");

            BH.Engine.Reflection.Compute.RecordWarning("ETABS handles gravity loads via loadcases, why only one gravity load per loadcase can be used. THis gravity load will be applied to all objects");
        }

        /***************************************************/

        public void SetLoad(BarTemperatureLoad barTemperatureLoad, bool replace)
        {
            double tempChange = barTemperatureLoad.TemperatureChange;

            int ret = 0;
            foreach (Bar bar in barTemperatureLoad.Objects.Elements)
            {
                string caseName = barTemperatureLoad.Loadcase.CustomData[AdapterId].ToString();
                string barName = bar.CustomData[AdapterId].ToString();
                ret = m_model.FrameObj.SetLoadTemperature(barName, caseName, 1, tempChange);
            }
        }

        /***************************************************/

        public bool CreateObject(ConstructionStage constructionStage)
        {
            eLoadPatternType patternType = eLoadPatternType.Construction;
            double selfWeight = 0;

            //Create Case and Change to a Construction Stage Case
            int ret = m_model.LoadPatterns.Add(constructionStage.Name, patternType, selfWeight, true);
            ret = m_model.LoadCases.StaticNonlinearStaged.SetCase(constructionStage.Name);

            //Create Stage Definitions
            int[] duration = new int [] { constructionStage.time, constructionStage.time, constructionStage.time };
            string[] comment = new string[] { "a", "b", "c"};
            ret = m_model.LoadCases.StaticNonlinearStaged.SetStageDefinitions(constructionStage.Name, 3, ref duration, ref comment);

            //Populate Stages
             csaAddFrames(constructionStage);
              csaRemoveFrames(constructionStage);
            csaGravityLoadFrames(constructionStage);

            return true;
        }

        /***************************************************/

        public bool CreateObject(ErectionSequence erectionSequence)
        {
            List<ConstructionStage> stages = erectionSequence.Stages;

            for(int i = 0; i< stages.Count(); i++)
            {
                CreateObject(stages[i]);
            }

            for (int i = 0; i < stages.Count(); i++)
            {
                if (i != 0)
                {
                    string currentCase = stages[i].Name;
                    string previousCase = stages[i - 1].Name;
                    int ret = m_model.LoadCases.StaticNonlinear.SetInitialCase(currentCase, previousCase);
                    ret = m_model.LoadCases.StaticNonlinearStaged.SetInitialCase(currentCase, previousCase);
                }
            }

            return true;
        }

        private void csaAddFrames(ConstructionStage constructionStage)
        {
            //Add Operations
            int size = constructionStage.addedMembers.Count();
            int[] operation = new int[size];
            string[] objectype = new string[size];
            string[] objectname = new string[size];
            double[] age = new double[size];
            string[] loadType = new string[size];
            string[] loadName = new string[size];
            double[] SF = new double[size];

            for (int i = 0; i < size; i++)
            {
                operation[i] = 1;
                objectype[i] = "Frame";
                objectname[i] = constructionStage.addedMembers[i].CustomData[AdapterId].ToString();
                age[i] = constructionStage.time;
                loadType[i] = "";
                loadName[i] = "";
                SF[i] = 1;
            }

             int  ret = m_model.LoadCases.StaticNonlinearStaged.SetStageData_2(constructionStage.Name, 1, size, ref operation, ref objectype, ref objectname, ref age, ref loadType, ref loadName, ref SF);
        }

        private void csaRemoveFrames(ConstructionStage constructionStage)
        {
            //Add Operations
            int size = constructionStage.deletedMembers.Count();
            int[] operation = new int[size];
            string[] objectype = new string[size];
            string[] objectname = new string[size];
            double[] age = new double[size];
            string[] loadType = new string[size];
            string[] loadName = new string[size];
            double[] SF = new double[size];

            for (int i = 0; i < size; i++)
            {
                operation[i] = 2;
                objectype[i] = "Frame";
                objectname[i] = constructionStage.deletedMembers[i].CustomData[AdapterId].ToString();
                age[i] = constructionStage.time;
                loadType[i] = "";
                loadName[i] = "";
                SF[i] = 1;
            }

            int ret = m_model.LoadCases.StaticNonlinearStaged.SetStageData_2(constructionStage.Name, 2, size, ref operation, ref objectype, ref objectname, ref age, ref loadType, ref loadName, ref SF);
        }

        private void csaGravityLoadFrames(ConstructionStage constructionStage)
        {
            //Add Operations
            int sizeLC = constructionStage.addedLoads.Count;
            int size = 0;

            for (int i = 0; i < sizeLC; i++)
            {
                GravityLoad load = (GravityLoad)constructionStage.addedLoads[i];
                size += load.Objects.Elements.Count;
            }

            int[] operation = new int[size];
            string[] objectype = new string[size];
            string[] objectname = new string[size];
            double[] age = new double[size];
            string[] loadType = new string[size];
            string[] loadName = new string[size];
            double[] SF = new double[size];
            int kounta = 0;

            for (int i = 0; i < sizeLC; i++)
            {
                GravityLoad load = (GravityLoad) constructionStage.addedLoads[i];

                for (int j = 0; j< load.Objects.Elements.Count; j++)
                {
                    operation[kounta] = 4;
                    objectype[kounta] = "Frame";
                    objectname[kounta] = load.Objects.Elements[j].CustomData[AdapterId].ToString();
                    age[kounta] = constructionStage.time;
                    loadType[kounta] = "Load";
                    loadName[kounta] = load.Loadcase.Name;
                    SF[kounta] =  1;
                    kounta++;
                }
            }

            int ret = m_model.LoadCases.StaticNonlinearStaged.SetStageData_2(constructionStage.Name, 3, size, ref operation, ref objectype, ref objectname, ref age, ref loadType, ref loadName, ref SF);
        }
    }
}
