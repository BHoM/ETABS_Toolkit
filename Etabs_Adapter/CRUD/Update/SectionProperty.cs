/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.SectionProperties;
using BH.Engine.Structure;
using BH.oM.Structure.Fragments;
using BH.Engine.Base;


namespace BH.Adapter.ETABS
{
#if Debug16 || Release16
    public partial class ETABS2016Adapter : BHoMAdapter
#elif Debug17 || Release17
   public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Update Panel                              ****/
        /***************************************************/
        
        private bool UpdateObjects(IEnumerable<ISectionProperty> bhSections)
        {
            bool success = true;

            int nameCount = 0;
            string[] names = { };
            m_model.PropFrame.GetNameList(ref nameCount, ref names);

            foreach (ISectionProperty bhSection in bhSections)
            {
                string propertyName = bhSection.DescriptionOrName();

                if (!names.Contains(propertyName))
                {
                    Engine.Base.Compute.RecordWarning($"Failed to update SectionPoperty: { propertyName }, no section with that name found in ETABS.");
                    continue;
                }

                // The API clamis that the Set methods for PropFrame are both initiasers and modifiers
                SetSection(bhSection as dynamic);

                SectionModifier modifier = bhSection.FindFragment<SectionModifier>();

                if (modifier != null)
                {
                    double[] etabsMods = new double[8];

                    etabsMods[0] = modifier.Area;   //Area
                    etabsMods[1] = modifier.Asz;    //Major axis shear
                    etabsMods[2] = modifier.Asy;    //Minor axis shear
                    etabsMods[3] = modifier.J;      //Torsion
                    etabsMods[4] = modifier.Iz;     //Minor bending
                    etabsMods[5] = modifier.Iy;     //Major bending
                    etabsMods[6] = 1;               //Mass, not currently implemented
                    etabsMods[7] = 1;               //Weight, not currently implemented

                    m_model.PropFrame.SetModifiers(propertyName, ref etabsMods);
                }
            }

            return success;
        }

        /***************************************************/

    }
}





