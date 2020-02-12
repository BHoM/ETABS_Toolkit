/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
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
                string caseName = pointLoad.Loadcase.CustomData[AdapterIdName].ToString();
                string nodeName = node.CustomData[AdapterIdName].ToString();
                ret = m_model.PointObj.SetLoadForce(nodeName, caseName, ref pfValues, replace);
            }
        }

        /***************************************************/

        public void SetLoad(BarUniformlyDistributedLoad barUniformLoad, bool replace)
        {

            foreach (Bar bar in barUniformLoad.Objects.Elements)
            {
                bool stepReplace = replace;

                string caseName = barUniformLoad.Loadcase.CustomData[AdapterIdName].ToString();
                string barName = bar.CustomData[AdapterIdName].ToString();

                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barUniformLoad.Force.X : direction == 2 ? barUniformLoad.Force.Y : barUniformLoad.Force.Z; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadDistributed(barName, caseName, 1, direction + 3, 0, 1, val, val, "Global", true, stepReplace);
                        stepReplace = false;
                    }

                }
                // Moment
                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barUniformLoad.Moment.X : direction == 2 ? barUniformLoad.Moment.Y : barUniformLoad.Moment.Z; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadDistributed(barName, caseName, 2, direction + 3, 0, 1, val, val, "Global", true, stepReplace);
                        stepReplace = false;
                    }

                }
            }
        }

        /***************************************************/

        public void SetLoad(AreaUniformlyDistributedLoad areaUniformLoad, bool replace)
        {
            int ret = 0;
            string caseName = areaUniformLoad.Loadcase.CustomData[AdapterIdName].ToString();
            foreach (IAreaElement area in areaUniformLoad.Objects.Elements)
            {
                bool tempReplace = replace;
                for (int direction = 1; direction <= 3; direction++)
                {
                    double val = direction == 1 ? areaUniformLoad.Pressure.X : direction == 2 ? areaUniformLoad.Pressure.Y : areaUniformLoad.Pressure.Z;
                    if (val != 0)
                    {
                        ret = m_model.AreaObj.SetLoadUniform(area.CustomData[AdapterIdName].ToString(), caseName, val, direction + 3, tempReplace);
                        tempReplace = false;
                    }
                }
            }
        }

        /***************************************************/

        public void SetLoad(BarVaryingDistributedLoad barLoad, bool replace)
        {
            int ret = 0;

            string caseName = barLoad.Loadcase.CustomData[AdapterIdName].ToString();

            foreach (Bar bar in barLoad.Objects.Elements)
            {
                bool stepReplace = replace;
                double val1, val2, dist1, dist2;
                string barName = bar.CustomData[AdapterIdName].ToString();
                for (int direction = 1; direction <= 3; direction++)
                {
                    double valA = direction == 1 ? barLoad.ForceA.X : direction == 2 ? barLoad.ForceA.Y : barLoad.ForceA.Z; //note: etabs acts different then stated in API documentstion
                    double valB = direction == 1 ? barLoad.ForceB.X : direction == 2 ? barLoad.ForceB.Y : barLoad.ForceB.Z; //note: etabs acts different then stated in API documentstion

                    if (bar.CheckFlipBar())
                    {
                        val1 = valB; //note: etabs acts different then stated in API documentstion
                        val2 = valA;
                        dist1 = barLoad.DistanceFromB;
                        dist2 = bar.Length() - barLoad.DistanceFromA;
                    }
                    else
                    {
                        val1 = valA; //note: etabs acts different then stated in API documentstion
                        val2 = valB;
                        dist1 = barLoad.DistanceFromA;
                        dist2 = bar.Length() - barLoad.DistanceFromB;
                    }

                    if (!(val1 == 0 && val2 == 0))
                    {
                        ret = m_model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterIdName].ToString(), caseName, 1, direction, dist1, dist2, val1, val2, "Global", false, stepReplace);
                        stepReplace = false;
                    }
                }
                // Moment
                for (int direction = 1; direction <= 3; direction++)
                {
                    double valA = direction == 1 ? barLoad.MomentA.X : direction == 2 ? barLoad.MomentA.Y : barLoad.MomentA.Z; //note: etabs acts different then stated in API documentstion
                    double valB = direction == 1 ? barLoad.MomentB.X : direction == 2 ? barLoad.MomentB.Y : barLoad.MomentB.Z; //note: etabs acts different then stated in API documentstion

                    if (bar.CheckFlipBar())
                    {
                        val1 = valB; //note: etabs acts different then stated in API documentstion
                        val2 = valA;
                        dist1 = barLoad.DistanceFromB;
                        dist2 = bar.Length() - barLoad.DistanceFromA;
                    }
                    else
                    {
                        val1 = valA; //note: etabs acts different then stated in API documentstion
                        val2 = valB;
                        dist1 = barLoad.DistanceFromA;
                        dist2 = bar.Length() - barLoad.DistanceFromB;
                    }

                    if (!(val1 == 0 && val2 == 0))
                    {
                        ret = m_model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterIdName].ToString(), caseName, 2, direction, dist1, dist2, val1, val2, "Global", false, stepReplace);
                        stepReplace = false;
                    }
                }
            }
        }

        /***************************************************/

        public void SetLoad(BarPointLoad barPointLoad, bool replace)
        {
            foreach (Bar bar in barPointLoad.Objects.Elements)
            {
                bool stepReplace = replace;

                string caseName = barPointLoad.Loadcase.CustomData[AdapterIdName].ToString();
                string barName = bar.CustomData[AdapterIdName].ToString();

                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barPointLoad.Force.X : direction == 2 ? barPointLoad.Force.Y : barPointLoad.Force.Z; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadPoint(barName, caseName, 1, direction + 3, barPointLoad.DistanceFromA, val, "Global", false, stepReplace);
                        stepReplace = false;
                    }
                }
                // Moment
                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barPointLoad.Moment.X : direction == 2 ? barPointLoad.Moment.Y : barPointLoad.Moment.Z; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadPoint(barName, caseName, 2, direction + 3, barPointLoad.DistanceFromA, val, "Global", false, stepReplace);
                        stepReplace = false;
                    }
                }
            }
        }

        /***************************************************/

        public void SetLoad(AreaTemperatureLoad areaTempratureLoad, bool replace)
        {
            int ret = 0;
            string caseName = areaTempratureLoad.Loadcase.CustomData[AdapterIdName].ToString();
            foreach (IAreaElement area in areaTempratureLoad.Objects.Elements)
            {
                double val = areaTempratureLoad.TemperatureChange;
                if (val != 0)
                    ret = m_model.AreaObj.SetLoadTemperature(area.CustomData[AdapterIdName].ToString(), caseName, 1, val, "", replace);
            }
        }

        /***************************************************/

        public void SetLoad(BarTemperatureLoad barTempratureLoad, bool replace)
        {
            int ret = 0;
            string caseName = barTempratureLoad.Loadcase.CustomData[AdapterIdName].ToString();
            foreach (Bar bar in barTempratureLoad.Objects.Elements)
            {
                double val = barTempratureLoad.TemperatureChange;
                if (val != 0)
                    ret = m_model.FrameObj.SetLoadTemperature(bar.CustomData[AdapterIdName].ToString(), caseName, 1, val, "", replace);
            }
        }

        /***************************************************/

        public void SetLoad(GravityLoad gravityLoad, bool replace)
        {
            double selfWeightMultiplier = 0;

            string caseName = gravityLoad.Loadcase.CustomData[AdapterIdName].ToString();

            m_model.LoadPatterns.GetSelfWTMultiplier(caseName, ref selfWeightMultiplier);

            if (selfWeightMultiplier != 0)
                BH.Engine.Reflection.Compute.RecordWarning($"Loadcase {gravityLoad.Loadcase.Name} allready had a selfweight multiplier which will get overridden. Previous value: {selfWeightMultiplier}, new value: {-gravityLoad.GravityDirection.Z}");

            m_model.LoadPatterns.SetSelfWTMultiplier(caseName, -gravityLoad.GravityDirection.Z);

            if (gravityLoad.GravityDirection.X != 0 || gravityLoad.GravityDirection.Y != 0)
                Engine.Reflection.Compute.RecordError("ETABS can only handle gravity loads in global z direction");

            BH.Engine.Reflection.Compute.RecordWarning("ETABS handles gravity loads via loadcases, why only one gravity load per loadcase can be used. THis gravity load will be applied to all objects");
        }
    }
}

