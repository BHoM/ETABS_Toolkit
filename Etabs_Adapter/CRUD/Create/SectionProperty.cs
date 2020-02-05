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
        /******     SectionProperty                  *******/
        /***************************************************/

        private bool CreateObject(ISectionProperty bhSection)
        {
            string propertyName = "None";
            if (bhSection.Name != "")
            {
                bhSection.CustomData[AdapterIdName] = propertyName = bhSection.Name;
            }
            else
            {
                BH.Engine.Reflection.Compute.RecordWarning("Section properties with no name will be converted to the null property 'None'.");
                bhSection.CustomData[AdapterIdName] = "None";
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
        /******     SetSection                       *******/
        /***************************************************/

        private void SetSection(IGeometricalSection section)
        {
            ISetProfile(section.SectionProfile, section.Name, section.Material);
        }

        /***************************************************/

        private void SetSection(ExplicitSection section)
        {
            m_model.PropFrame.SetGeneral(section.Name, section.Material.Name, section.Vz + section.Vpz, section.Vy + section.Vpy,
                section.Area, section.Asz, section.Asy, section.J,
                section.Iz, section.Iy,     // I22, I33
                section.Welz, section.Wely, // S22, S33
                section.Wplz, section.Wply, // Z22, Z33
                section.Rgz, section.Rgy);  // R22, R33
        }

        /***************************************************/

        private void SetSection(ISectionProperty section)
        {
            CreateElementError(section.GetType().ToString(), section.Name);
        }

        /***************************************************/
        /******     SetProfile                       *******/
        /***************************************************/

        private void ISetProfile(IProfile sectionProfile, string sectionName, IMaterialFragment material)
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

        private void SetProfile(IProfile profile, string sectionName, IMaterialFragment material)
        {
            CreateElementError(profile.GetType().ToString(), sectionName);
        }

        /***************************************************/
    }
}