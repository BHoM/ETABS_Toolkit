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
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
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

            if (!GetFromDatabase(bhSection))
            {
                SetSection(bhSection as dynamic);
            }

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


        private bool GetFromDatabase(ISectionProperty bhSection)
        {
            if (EtabsSettings.DatabaseSettings.SectionDatabase == SectionDatabase.None)
                return false;

            string bhName = bhSection.Name;

            bhName = bhName.ToUpper();
            bhName = bhName.Replace(" ", "");

            string[] sub = bhName.Split('X');

            for (int i = 0; i < sub.Length; i++)
            {
                if (sub[i].EndsWith(".0"))
                    sub[i] = string.Join("", sub[i].Take(sub[i].Length - 2).ToArray());
            }

            bhName = string.Join("X", sub);

            foreach (KeyValuePair<string, string> exch in GetDictionary(EtabsSettings.DatabaseSettings.SectionDatabase))
            {
                if (bhName.StartsWith(exch.Key))
                {
                    bhName = bhName.Replace(exch.Key, exch.Value);
                    break;
                }
            }


            if (!m_DBSectionsNames.Contains(bhName))
                return false;

            if (1 == m_model.PropFrame.ImportProp(
                                                bhName,
                                                bhSection.Material.Name,
                                                EnumToString(EtabsSettings.DatabaseSettings.SectionDatabase),
                                                bhName))
            {
                return false;
            }

            Engine.Reflection.Compute.RecordNote(bhSection.Name + " properties has been assigned from the database as " + bhName + ".");
            return true;
        }

        /***************************************************/

        private Dictionary<string, string> GetDictionary(SectionDatabase sectionDatabase)
        {
            switch (sectionDatabase)
            {
                case SectionDatabase.BSShapes2006:
                    return new Dictionary<string, string>()
                    {
                        { "UB",  "UKB" },
                        { "UC",  "UKC" },
                        { "UBP", "UKBP" },
                        { "L",   "UKA" },
                        { "PFC", "UKPFC" },
                        { "CHS", "CHHF" },
                        { "RHS", "RHHF" },
                        { "SHS", "SHHF" },
                        { "TUB", "UKT" },
                        { "TUC", "UKT" }
                    };
                case SectionDatabase.None:
                case SectionDatabase.AISC14:
                case SectionDatabase.AISC14M:
                case SectionDatabase.AISC15:
                case SectionDatabase.AISC15M:
                case SectionDatabase.ArcelorMittal_British:
                case SectionDatabase.ArcelorMittal_BritishHISTAR:
                case SectionDatabase.ArcelorMittal_Europe:
                case SectionDatabase.ArcelorMittal_EuropeHISTAR:
                case SectionDatabase.ArcelorMittal_Japan:
                case SectionDatabase.ArcelorMittal_Russia:
                case SectionDatabase.ArcelorMittal_US_ASTM_A913:
                case SectionDatabase.ArcelorMittal_US_ASTM_A913M:
                case SectionDatabase.ArcelorMittal_US_ASTM_A992:
                case SectionDatabase.ArcelorMittal_US_ASTM_A992M:
                case SectionDatabase.Australia_NewZealand:
                case SectionDatabase.ChineseGB08:
                case SectionDatabase.CISC9:
                case SectionDatabase.CISC10:
                case SectionDatabase.CoreBraceBRB_2016:
                case SectionDatabase.Euro:
                case SectionDatabase.Indian:
                case SectionDatabase.JIS_G_3192_2014:
                case SectionDatabase.Nordic:
                case SectionDatabase.Russian:
                case SectionDatabase.SJIJoists:
                case SectionDatabase.StarSeismicBRB:
                default:
                    return new Dictionary<string, string>();
            }
        }

        /***************************************************/

        private string EnumToString(SectionDatabase sectionDB)
        {
            switch (sectionDB)
            {
                case SectionDatabase.AISC14:
                     return "AISC14.xml";
                case SectionDatabase.AISC14M:
                     return "AISC14.xml";
                case SectionDatabase.AISC15:
                     return "AISC15.xml";
                case SectionDatabase.AISC15M:
                     return "AISC15M.xml";
                case SectionDatabase.ArcelorMittal_British:
                     return "ArcelorMittal_British.xml";
                case SectionDatabase.ArcelorMittal_BritishHISTAR:
                     return "ArcelorMittal_BritishHISTAR.xml";
                case SectionDatabase.ArcelorMittal_Europe:
                     return "ArcelorMittal_Europe.xml";
                case SectionDatabase.ArcelorMittal_EuropeHISTAR:
                     return "ArcelorMittal_EuropeHISTAR.xml";
                case SectionDatabase.ArcelorMittal_Japan:
                     return "ArcelorMittal_Japan.xml";
                case SectionDatabase.ArcelorMittal_Russia:
                     return "ArcelorMittal_Russia.xml";
                case SectionDatabase.ArcelorMittal_US_ASTM_A913:
                     return "ArcelorMittal_US_ASTM-A913.xml";
                case SectionDatabase.ArcelorMittal_US_ASTM_A913M:
                     return "ArcelorMittal_US_ASTM-A913M.xml";
                case SectionDatabase.ArcelorMittal_US_ASTM_A992:
                     return "ArcelorMittal_US_ASTM-A992.xml";
                case SectionDatabase.ArcelorMittal_US_ASTM_A992M:
                     return "ArcelorMittal_US_ASTM-A992M.xml";
                case SectionDatabase.Australia_NewZealand:
                     return "Australia-NewZealand.xml";
                case SectionDatabase.BSShapes2006:
                     return "BSShapes2006.xml";
                case SectionDatabase.ChineseGB08:
                     return "ChineseGB08.xml";
                case SectionDatabase.CISC9:
                     return "CISC9.xml";
                case SectionDatabase.CISC10:
                     return "CISC10.xml";
                case SectionDatabase.CoreBraceBRB_2016:
                     return "CoreBraceBRB_2016.xml";
                case SectionDatabase.Euro:
                     return "Euro.xml";
                case SectionDatabase.Indian:
                     return "Indian.xml";
                case SectionDatabase.JIS_G_3192_2014:
                     return "JIS-G-3192-2014.xml";
                case SectionDatabase.Nordic:
                     return "Nordic.xml";
                case SectionDatabase.Russian:
                     return "Russian.xml";
                case SectionDatabase.SJIJoists:
                     return "SJIJoists.xml";
                case SectionDatabase.StarSeismicBRB:
                     return "StarSeismicBRB.xml";
                default:
                     return "";
            }
        }

        /***************************************************/
    }
}