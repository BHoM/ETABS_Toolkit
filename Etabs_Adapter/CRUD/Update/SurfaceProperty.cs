/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
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
using BH.Engine.Structure;
using BH.oM.Structure.SurfaceProperties;

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
        
        private bool UpdateObjects(IEnumerable<ISurfaceProperty> bhSurfaceProperties)
        {
            bool success = true;

            int nameCount = 0;
            string[] nameArr = { };
            m_model.PropArea.GetNameList(ref nameCount, ref nameArr);

            foreach (ISurfaceProperty property2d in bhSurfaceProperties)
            {
                if (!nameArr.Contains(property2d.DescriptionOrName()))
                {
                    Engine.Reflection.Compute.RecordWarning($"Failed to update SurfaceProperty: { property2d.DescriptionOrName() }, no surface property with that name found in ETABS.");
                    continue;
                }

                // The API claims that "set" is both initilizer and modifier
                success &= CreateObject(property2d);
            }

            return success;
        }

        /***************************************************/

    }
}


