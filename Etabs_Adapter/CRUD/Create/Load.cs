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
using BH.Engine.Spatial;
using BH.Engine.Adapters.ETABS;

#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /****       Create Methods                      ****/
        /***************************************************/

        private bool CreateObject(ILoad bhLoad)
        {
            SetLoad(bhLoad as dynamic, this.EtabsSettings.ReplaceLoads);

            return true;
        }

        /***************************************************/

        public void SetLoad(PointLoad pointLoad, bool replace)
        {
            if (pointLoad.Axis != LoadAxis.Global)
                Engine.Reflection.Compute.RecordWarning("The local coordinate system of BHoM nodes are not pushed to ETABS, Local Axis PointLoads are set on the Global Axis");

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
            string axis;
            int shift;
            GetDirectionData(barUniformLoad, out axis, out shift);
            foreach (Bar bar in barUniformLoad.Objects.Elements)
            {
                bool stepReplace = replace;

                string caseName = barUniformLoad.Loadcase.CustomData[AdapterIdName].ToString();
                string barName = bar.CustomData[AdapterIdName].ToString();

                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barUniformLoad.Force.X : barUniformLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? barUniformLoad.Force.Y : (shift == 6 ? -barUniformLoad.Force.Z : barUniformLoad.Force.Z) :
                        direction == 2 ? barUniformLoad.Force.Z : -barUniformLoad.Force.Y; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadDistributed(barName, caseName, 1, direction + shift, 0, 1, val, val, axis, true, stepReplace);
                        stepReplace = false;
                    }

                }
                // Moment
                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barUniformLoad.Moment.X : barUniformLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? barUniformLoad.Moment.Y : (shift == 6 ? -barUniformLoad.Moment.Z : barUniformLoad.Moment.Z) :
                        direction == 2 ? barUniformLoad.Moment.Z : -barUniformLoad.Moment.Y; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadDistributed(barName, caseName, 2, direction + shift, 0, 1, val, val, axis, true, stepReplace);
                        stepReplace = false;
                    }

                }
            }
        }

        /***************************************************/

        public void SetLoad(AreaUniformlyDistributedLoad areaUniformLoad, bool replace)
        {
            string axis;
            int shift;
            GetDirectionData(areaUniformLoad, out axis, out shift);
            int ret = 0;
            string caseName = areaUniformLoad.Loadcase.CustomData[AdapterIdName].ToString();
            foreach (IAreaElement area in areaUniformLoad.Objects.Elements)
            {
                bool tempReplace = replace;
                for (int direction = 1; direction <= 3; direction++)
                {
                    double val = direction == 1 ? areaUniformLoad.Pressure.X : areaUniformLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? areaUniformLoad.Pressure.Y : (shift == 6 ? -areaUniformLoad.Pressure.Z : areaUniformLoad.Pressure.Z) :
                        direction == 2 ? -areaUniformLoad.Pressure.Y : areaUniformLoad.Pressure.Z; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.AreaObj.SetLoadUniform(area.CustomData[AdapterIdName].ToString(), caseName, val, direction + shift, tempReplace, axis);
                        tempReplace = false;
                    }
                }
            }
        }

        /***************************************************/

        public void SetLoad(BarVaryingDistributedLoad barLoad, bool replace)
        {
            string axis;
            int shift;
            GetDirectionData(barLoad, out axis, out shift);
            int ret = 0;
            string caseName = barLoad.Loadcase.CustomData[AdapterIdName].ToString();

            foreach (Bar bar in barLoad.Objects.Elements)
            {
                bool stepReplace = replace;
                double val1, val2, dist1, dist2;
                string barName = bar.CustomData[AdapterIdName].ToString();
                for (int direction = 1; direction <= 3; direction++)
                {
                    double valA = direction == 1 ? barLoad.ForceA.X : barLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? barLoad.ForceA.Y : (shift == 6 ? -barLoad.ForceA.Z : barLoad.ForceA.Z) :
                        direction == 2 ? barLoad.ForceA.Z : -barLoad.ForceA.Y; //note: etabs acts different then stated in API documentstion
                    double valB = direction == 1 ? barLoad.ForceB.X : barLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? barLoad.ForceB.Y : (shift == 6 ? -barLoad.ForceB.Z : barLoad.ForceB.Z) :
                        direction == 2 ? barLoad.ForceB.Z : -barLoad.ForceB.Y; //note: etabs acts different then stated in API documentstion

                    val1 = valA; //note: etabs acts different then stated in API documentstion
                    val2 = valB;
                    dist1 = barLoad.DistanceFromA;
                    dist2 = bar.Length() - barLoad.DistanceFromB;

#if Debug16 || Release16
                    if (bar.CheckFlipBar())
                    {
                        val1 = valB; //note: etabs acts different then stated in API documentstion
                        val2 = valA;
                        dist1 = barLoad.DistanceFromB;
                        dist2 = bar.Length() - barLoad.DistanceFromA;
                    }
#endif

                    if (!(val1 == 0 && val2 == 0))
                    {
                        if (val1 * val2 < 0)
                            Engine.Reflection.Compute.RecordWarning("BarVaryingLoad can not be in opposite directions for the two end values");
                        else
                        {
                            ret = m_model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterIdName].ToString(), caseName, 1, direction + shift, dist1, dist2, val1, val2, axis, false, stepReplace);
                            stepReplace = false;
                        }
                    }
                }
                // Moment
                for (int direction = 1; direction <= 3; direction++)
                {
                    double valA = direction == 1 ? barLoad.MomentA.X : barLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? barLoad.MomentA.Y : (shift == 6 ? -barLoad.MomentA.Z : barLoad.MomentA.Z) :
                        direction == 2 ? barLoad.MomentA.Z : -barLoad.MomentA.Y; //note: etabs acts different then stated in API documentstion
                    double valB = direction == 1 ? barLoad.MomentB.X : barLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? barLoad.MomentB.Y : (shift == 6 ? -barLoad.MomentB.Z : barLoad.MomentB.Z) :
                        direction == 2 ? barLoad.MomentB.Z : -barLoad.MomentB.Y; //note: etabs acts different then stated in API documentstion

                    val1 = valA; //note: etabs acts different then stated in API documentstion
                    val2 = valB;
                    dist1 = barLoad.DistanceFromA;
                    dist2 = bar.Length() - barLoad.DistanceFromB;

#if Debug16 || Release16
                    if (bar.CheckFlipBar())
                    {
                        val1 = valB; //note: etabs acts different then stated in API documentstion
                        val2 = valA;
                        dist1 = barLoad.DistanceFromB;
                        dist2 = bar.Length() - barLoad.DistanceFromA;
                    }
#endif

                    if (!(val1 == 0 && val2 == 0))
                    {
                        ret = m_model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterIdName].ToString(), caseName, 2, direction + shift, dist1, dist2, val1, val2, axis, false, stepReplace);
                        stepReplace = false;
                    }
                }
            }
        }

        /***************************************************/

        public void SetLoad(BarPointLoad barPointLoad, bool replace)
        {
            string axis;
            int shift;
            GetDirectionData(barPointLoad, out axis, out shift);
            foreach (Bar bar in barPointLoad.Objects.Elements)
            {
                bool stepReplace = replace;

                string caseName = barPointLoad.Loadcase.CustomData[AdapterIdName].ToString();
                string barName = bar.CustomData[AdapterIdName].ToString();

                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barPointLoad.Force.X : barPointLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? barPointLoad.Force.Y : barPointLoad.Force.Z :
                        direction == 2 ? barPointLoad.Force.Z : -barPointLoad.Force.Y; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadPoint(barName, caseName, 1, direction + shift, barPointLoad.DistanceFromA, val, axis, false, stepReplace);
                        stepReplace = false;
                    }
                }
                // Moment
                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barPointLoad.Moment.X : barPointLoad.Axis == LoadAxis.Global ?
                        direction == 2 ? barPointLoad.Moment.Y : barPointLoad.Moment.Z :
                        direction == 2 ? barPointLoad.Moment.Z : -barPointLoad.Moment.Y; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        ret = m_model.FrameObj.SetLoadPoint(barName, caseName, 2, direction + shift, barPointLoad.DistanceFromA, val, axis, false, stepReplace);
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

        /***************************************************/
        /****       Helper Methods                      ****/
        /***************************************************/

        private void GetDirectionData(ILoad load, out string axis, out int shift)
        {
            if (load.Axis == LoadAxis.Local)
            {
                axis = "Local";
                shift = 0;
            }
            else
            {
                axis = "Global";
                shift = load.Projected ? 6 : 3;
            }
        }

        /***************************************************/

    }
}

