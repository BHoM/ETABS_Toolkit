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

#if (Debug2017)
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
                cases[i] = massSource.FactoredAdditionalCases[i].Item1.CustomData[AdapterId].ToString();
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
            return false;
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
                    double dist2 = barLoad.DistanceFromB;
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
                Engine.Reflection.Compute.RecordError("Etabs can only handle gravity loads in global z direction");

            BH.Engine.Reflection.Compute.RecordWarning("Etabs handles gravity loads via loadcases, why only one gravity load per loadcase can be used. THis gravity load will be applied to all objects");
        }
    }
}
