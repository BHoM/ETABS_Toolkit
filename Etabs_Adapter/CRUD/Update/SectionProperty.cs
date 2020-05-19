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
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Loads;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Adapter;
using BH.oM.Geometry;

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
                    Engine.Reflection.Compute.RecordWarning($"Failed to update SectionPoperty: { propertyName }, no section with that name found in ETABS.");
                    continue;
                }

                // The API clamis that the Set methods for PropFrame are both initiasers and modifiers
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

                    m_model.PropFrame.SetModifiers(propertyName, ref etabsMods);
                }
            }

            return success;
        }

        /***************************************************/

    }
}
