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
using BH.oM.Geometry.SettingOut;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Loads;
using BH.oM.Structure.Offsets;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Geometry.ShapeProfiles;
using BH.oM.Geometry;
using System.ComponentModel;
using BH.oM.Adapters.ETABS;
using BH.Engine.Base;
using System;
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

        private bool CreateObject(Bar bhBar)
        {
            int ret = 0;


            string name = "";
            
            ret = m_model.FrameObj.AddByPoint(bhBar.StartNode.CustomData[AdapterIdName].ToString(), bhBar.EndNode.CustomData[AdapterIdName].ToString(), ref name);
            
            bhBar.CustomData[AdapterIdName] = name;

            if (ret != 0)
            {
                CreateElementError("Bar", name);
                return false;
            }

            return SetObject(bhBar);
        }

        /***************************************************/

        [Description("Does all the ETABS interaction which does not initiate a new object in ETABS.")]
        private bool SetObject(Bar bhBar)
        {
            int ret = 0;
            string name = bhBar.CustomData[AdapterIdName].ToString();

            // Needed rigth after create as well as AddByPoint flipps the Bar if it feels like it
            m_model.EditFrame.ChangeConnectivity(name, bhBar.StartNode.CustomData[AdapterIdName].ToString(), bhBar.EndNode.CustomData[AdapterIdName].ToString());

            if (bhBar.SectionProperty != null)
            {
                if (m_model.FrameObj.SetSection(name, bhBar.SectionProperty.CustomData[AdapterIdName].ToString()) != 0)
                {
                    CreatePropertyWarning("SectionProperty", "Bar", name);
                    ret++;
                }
            }

            if (m_model.FrameObj.SetLocalAxes(name, bhBar.OrientationAngle * 180 / System.Math.PI) != 0)
            {
                CreatePropertyWarning("Orientation angle", "Bar", name);
                ret++;
            }

            Offset offset = bhBar.Offset;

            double[] offset1 = new double[3];
            double[] offset2 = new double[3];

            if (offset != null)
            {
                offset1[1] = offset.Start.Z;
                offset1[2] = offset.Start.Y;
                offset2[1] = offset.End.Z;
                offset2[2] = offset.End.Y;
            }

            if (m_model.FrameObj.SetInsertionPoint(name, (int)bhBar.InsertionPoint(), false, bhBar.ModifyStiffnessInsertionPoint(), ref offset1, ref offset2) != 0)
            {
                CreatePropertyWarning("insertion point and perpendicular offset", "Bar", name);
                ret++;
            }

            if (bhBar.Release != null)
            {
                bool[] restraintStart = null;
                double[] springStart = null;
                bool[] restraintEnd = null;
                double[] springEnd = null;

                if (bhBar.Release.ToCSI(ref restraintStart, ref springStart, ref restraintEnd, ref springEnd))
                {
                    if (m_model.FrameObj.SetReleases(name, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd) != 0)
                    {
                        CreatePropertyWarning("Release", "Bar", name);
                        ret++;
                    }
                }
            }

            AutoLengthOffset autoLengthOffset = bhBar.AutoLengthOffset();
            if (autoLengthOffset != null)
            {
                //the Rigid Zone Factor is not picked up when setting the auto length = true for the method call. Hence need to call this method twice.
                int retAutoLEngthOffset = m_model.FrameObj.SetEndLengthOffset(name, false, 0, 0, autoLengthOffset.RigidZoneFactor);
                retAutoLEngthOffset += m_model.FrameObj.SetEndLengthOffset(name, autoLengthOffset.AutoOffset, 0, 0, 0);
                if (retAutoLEngthOffset != 0)
                {
                    CreatePropertyWarning("Auto length offset", "Bar", name);
                    ret++;
                }
            }
            else if (bhBar.Offset != null)
            {
                if (m_model.FrameObj.SetEndLengthOffset(name, false, bhBar.Offset.Start.X, -1 * (bhBar.Offset.End.X), 1) != 0)
                {
                    CreatePropertyWarning("Length offset", "Bar", name);
                    ret++;
                }
            }

            return true;
        }

        /***************************************************/

    }
}

