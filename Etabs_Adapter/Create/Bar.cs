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
using BH.oM.Architecture.Elements;
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
#if Debug2017
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
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

            if (m_model.FrameObj.SetSection(name, bhBar.SectionProperty.Name) != 0)
            {
                CreatePropertyWarning("SectionProperty", "Bar", name);
                ret++;
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

            BarRelease barRelease = bhBar.Release;
            if (barRelease != null)
            {
                bool[] restraintStart = null;// = barRelease.StartRelease.Fixities();// Helper.GetRestraint6DOF(barRelease.StartRelease);
                double[] springStart = null;// = barRelease.StartRelease.ElasticValues();// Helper.GetSprings6DOF(barRelease.StartRelease);
                bool[] restraintEnd = null;// = barRelease.EndRelease.Fixities();// Helper.GetRestraint6DOF(barRelease.EndRelease);
                double[] springEnd = null;// = barRelease.EndRelease.ElasticValues();// Helper.GetSprings6DOF(barRelease.EndRelease);


                bhBar.GetCSIBarRelease(ref restraintStart, ref springStart, ref restraintEnd, ref springEnd);

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

            return ret == 0;
        }

        /***************************************************/

        private bool CreateObject(ISectionProperty bhSection)
        {
            bool success = true;

            SetSection(bhSection);

            return success;
        }

        /***************************************************/

        private bool SetSection(ISectionProperty bhSection)
        {
            //without modelData and the adapter there is no reason to devide this into SetSection and SetSectionProperty... right?
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
        }

        /***************************************************/

        private void SetSection(CompositeSection section)
        {
            //contains SteelSection and ConcreteScetion
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

        private void SetProfile(TubeProfile dimensions, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetPipe(sectionName, material.Name, dimensions.Diameter, dimensions.Thickness);
        }

        /***************************************************/

        private void SetProfile(BoxProfile dimensions, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetTube(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.Thickness, dimensions.Thickness);
        }

        /***************************************************/

        private void SetProfile(FabricatedBoxProfile dimensions, string sectionName, IMaterialFragment material)
        {
            if (dimensions.TopFlangeThickness != dimensions.BotFlangeThickness)
                Engine.Reflection.Compute.RecordWarning("different thickness of top and bottom flange is not supported in ETABS");
            m_model.PropFrame.SetTube(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.TopFlangeThickness, dimensions.WebThickness);
        }

        /***************************************************/

        private void SetProfile(ISectionProfile dimensions, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetISection(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.Width, dimensions.FlangeThickness);
        }

        /***************************************************/

        private void SetProfile(FabricatedISectionProfile dimensions, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetISection(sectionName, material.Name, dimensions.Height, dimensions.TopFlangeWidth, dimensions.TopFlangeThickness, dimensions.WebThickness, dimensions.BotFlangeWidth, dimensions.BotFlangeThickness);
        }

        /***************************************************/

        private void SetProfile(ChannelProfile dimensions, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetChannel(sectionName, material.Name, dimensions.Height, dimensions.FlangeWidth, dimensions.FlangeThickness, dimensions.WebThickness);
            if (dimensions.MirrorAboutLocalZ)
                RecordFlippingError(sectionName);
        }

        /***************************************************/

        private void SetProfile(AngleProfile dimensions, string sectionName, IMaterialFragment material)
        {

            if (material is Steel || material is Aluminium)
                m_model.PropFrame.SetSteelAngle(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.RootRadius, dimensions.MirrorAboutLocalZ, dimensions.MirrorAboutLocalY);
            else if (material is Concrete)
                m_model.PropFrame.SetConcreteL(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.WebThickness, dimensions.MirrorAboutLocalZ, dimensions.MirrorAboutLocalY);
            else
            {
                m_model.PropFrame.SetAngle(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness);
                if (dimensions.MirrorAboutLocalY || dimensions.MirrorAboutLocalZ)
                    RecordFlippingError(sectionName);
            }

        }

        /***************************************************/

        private void SetProfile(TSectionProfile dimensions, string sectionName, IMaterialFragment material)
        {
            if (material is Steel || material is Aluminium)
                m_model.PropFrame.SetSteelTee(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.RootRadius, dimensions.MirrorAboutLocalY);
            else if (material is Concrete)
                m_model.PropFrame.SetConcreteTee(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.WebThickness, dimensions.MirrorAboutLocalY);
            else
            {
                m_model.PropFrame.SetTee(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness);
                if (dimensions.MirrorAboutLocalY)
                    RecordFlippingError(sectionName);
            }

        }

        /***************************************************/

        private void SetProfile(ZSectionProfile dimensions, string sectionName, IMaterialFragment material)
        {
            Engine.Reflection.Compute.RecordWarning("Z-Section currently not supported in the Etabs adapter. Section with name " + sectionName + " has not been pushed.");
        }

        /***************************************************/

        private void SetProfile(RectangleProfile dimensions, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetRectangle(sectionName, material.Name, dimensions.Height, dimensions.Width);
        }

        /***************************************************/

        private void SetProfile(CircleProfile dimensions, string sectionName, IMaterialFragment material)
        {
            m_model.PropFrame.SetCircle(sectionName, material.Name, dimensions.Diameter);
        }

        /***************************************************/
    }
}
