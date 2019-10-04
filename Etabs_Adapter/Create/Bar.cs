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

        private bool CreateObject(Bar bhBar)
        {
            int ret = 0;


            string name = "";
            
            ret = m_model.FrameObj.AddByPoint(bhBar.StartNode.CustomData[AdapterId].ToString(), bhBar.EndNode.CustomData[AdapterId].ToString(), ref name);

            bhBar.CustomData[AdapterId] = name;

            if (ret != 0)
            {
                CreateElementError("Bar", name);
                return false;
            }

            if (bhBar.SectionProperty != null)
            {
                if (m_model.FrameObj.SetSection(name, bhBar.SectionProperty.CustomData[AdapterId].ToString()) != 0)
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

            if (m_model.FrameObj.SetInsertionPoint(name, (int)bhBar.InsertionPoint(), false, true, ref offset1, ref offset2) != 0)
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

                bhBar.Release.ToCSI(ref restraintStart, ref springStart, ref restraintEnd, ref springEnd);
                
                if (m_model.FrameObj.SetReleases(name, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd) != 0)
                {
                    CreatePropertyWarning("Release", "Bar", name);
                    ret++;
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
                if (m_model.FrameObj.SetEndLengthOffset(name, false, -1 * (bhBar.Offset.Start.X), bhBar.Offset.End.X, 1) != 0)
                {
                    CreatePropertyWarning("Length offset", "Bar", name);
                    ret++;
                }
            }

            return true;
        }

        /***************************************************/

        private bool CreateObject(ISectionProperty bhSection)
        {
            string propertyName = "None";
            if (bhSection.Name != "")
            {
                bhSection.CustomData[AdapterId] = propertyName = bhSection.Name;
            }
            else
            {
                BH.Engine.Reflection.Compute.RecordWarning("Section properties with no name will be converted to the null property 'None'.");
                bhSection.CustomData[AdapterId] = "None";
                return true;
            }

            SetSection(bhSection as dynamic);

            double[] modifiers = bhSection.Modifiers();

            if (modifiers != null)
            {
                double[] etabsMods = new double[8];

                etabsMods[0] = modifiers[0];    //Area
                etabsMods[1] = modifiers[4];    //Minor axis shear
                etabsMods[2] = modifiers[5];    //Major axis shear
                etabsMods[3] = modifiers[3];    //Torsion
                etabsMods[4] = modifiers[1];    //Major bending
                etabsMods[5] = modifiers[2];    //Minor bending
                etabsMods[6] = 1;               //Mass, not currently implemented
                etabsMods[7] = 1;               //Weight, not currently implemented

                m_model.PropFrame.SetModifiers(bhSection.Name, ref etabsMods);
            }

            return true;
        }

        /***************************************************/

        private void SetSection(SteelSection section)
        {
            SetProfile(section.SectionProfile, section.Name, section.Material);
        }

        /***************************************************/

        private void SetSection(ConcreteSection section)
        {
            SetProfile(section.SectionProfile, section.Name, section.Material);
        }

        /***************************************************/

        private void SetSection(CableSection section)
        {
            //no ISectionDimentions
            CreateElementError("Section Profile", section.Name);
        }

        /***************************************************/

        private void SetSection(CompositeSection section)
        {
            //contains SteelSection and ConcreteScetion
            CreateElementError("Section Profile", section.Name);
        }

        /***************************************************/

        private void SetSection(ExplicitSection section)
        {
            m_model.PropFrame.SetGeneral(section.Name, section.Material.Name, section.CentreZ * 2, section.CentreY * 2, section.Area, section.Asy, section.Asz, section.J, section.Iy, section.Iz, section.Wply, section.Wplz, section.Wely, section.Wely, section.Rgy, section.Rgz);
        }

        /***************************************************/

        private void SetProfile(IProfile sectionProfile, string sectionName, IMaterialFragment material)
        {
            SetProfile(sectionProfile as dynamic, sectionName, material);
        }

        /***************************************************/

        private void SetProfile(TubeProfile profile, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetPipe(sectionName, material.Name, profile.Diameter, profile.Thickness);
        }

        /***************************************************/

        private void SetProfile(BoxProfile profile, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetTube(sectionName, material.Name, profile.Height, profile.Width, profile.Thickness, profile.Thickness);
        }

        /***************************************************/

        private void SetProfile(FabricatedBoxProfile profile, string sectionName, IMaterialFragment material)
        {
            if (profile.TopFlangeThickness != profile.BotFlangeThickness)
                Engine.Reflection.Compute.RecordWarning("different thickness of top and bottom flange is not supported in ETABS");
            m_model.PropFrame.SetTube(sectionName, material.Name, profile.Height, profile.Width, profile.TopFlangeThickness, profile.WebThickness);
        }

        /***************************************************/

        private void SetProfile(ISectionProfile profile, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetISection(sectionName, material.Name, profile.Height, profile.Width, profile.FlangeThickness, profile.WebThickness, profile.Width, profile.FlangeThickness);
        }

        /***************************************************/

        private void SetProfile(FabricatedISectionProfile profile, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetISection(sectionName, material.Name, profile.Height, profile.TopFlangeWidth, profile.TopFlangeThickness, profile.WebThickness, profile.BotFlangeWidth, profile.BotFlangeThickness);
        }

        /***************************************************/

        private void SetProfile(ChannelProfile profile, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetChannel(sectionName, material.Name, profile.Height, profile.FlangeWidth, profile.FlangeThickness, profile.WebThickness);
            if (profile.MirrorAboutLocalZ)
                RecordFlippingError(sectionName);
        }

        /***************************************************/

        private void SetProfile(AngleProfile profile, string sectionName, IMaterialFragment material)
        {

            if (material is Steel || material is Aluminium)
                m_model.PropFrame.SetSteelAngle(sectionName, material.Name, profile.Height, profile.Width, profile.FlangeThickness, profile.WebThickness, profile.RootRadius, profile.MirrorAboutLocalZ, profile.MirrorAboutLocalY);
            else if (material is Concrete)
                m_model.PropFrame.SetConcreteL(sectionName, material.Name, profile.Height, profile.Width, profile.FlangeThickness, profile.WebThickness, profile.WebThickness, profile.MirrorAboutLocalZ, profile.MirrorAboutLocalY);
            else
            {
                m_model.PropFrame.SetAngle(sectionName, material.Name, profile.Height, profile.Width, profile.FlangeThickness, profile.WebThickness);
                if (profile.MirrorAboutLocalY || profile.MirrorAboutLocalZ)
                    RecordFlippingError(sectionName);
            }

        }

        /***************************************************/

        private void SetProfile(TSectionProfile profile, string sectionName, IMaterialFragment material)
        {
            if (material is Steel || material is Aluminium)
                m_model.PropFrame.SetSteelTee(sectionName, material.Name, profile.Height, profile.Width, profile.FlangeThickness, profile.WebThickness, profile.RootRadius, profile.MirrorAboutLocalY);
            else if (material is Concrete)
                m_model.PropFrame.SetConcreteTee(sectionName, material.Name, profile.Height, profile.Width, profile.FlangeThickness, profile.WebThickness, profile.WebThickness, profile.MirrorAboutLocalY);
            else
            {
                m_model.PropFrame.SetTee(sectionName, material.Name, profile.Height, profile.Width, profile.FlangeThickness, profile.WebThickness);
                if (profile.MirrorAboutLocalY)
                    RecordFlippingError(sectionName);
            }

        }

        /***************************************************/

        private void SetProfile(ZSectionProfile profile, string sectionName, IMaterialFragment material)
        {
            Engine.Reflection.Compute.RecordWarning("Z-Section currently not supported in the ETABS adapter. Section with name " + sectionName + " has not been pushed.");
        }

        /***************************************************/

        private void SetProfile(RectangleProfile profile, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetRectangle(sectionName, material.Name, profile.Height, profile.Width);
        }

        /***************************************************/

        private void SetProfile(CircleProfile profile, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetCircle(sectionName, material.Name, profile.Diameter);
        }

        /***************************************************/

        private void SetProfile(FreeFormProfile profile, string sectionName, IMaterialFragment material)
        {
            CreateElementError("Section Profile", sectionName);
        }

        /***************************************************/
    }
}
