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

using BH.oM.Geometry.SettingOut;
using System.Collections.Generic;
using System.Linq;
#if Debug18 || Release18
#elif Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug18 || Release18
    public partial class ETABS18Adapter : BHoMAdapter
#elif Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/

        private bool CreateCollection(IEnumerable<Level> levels)
        {
            int count = levels.Count();
            if (count < 1)
                return true;

            List<Level> levelList = levels.OrderBy(x => x.Elevation).ToList();

            if (levelList.Any(x => string.IsNullOrWhiteSpace(x.Name)))
                Engine.Reflection.Compute.RecordWarning("Unnamed levels have been given name according to their height index: Level 'i'");

            string[] names = levelList.Select((x, i) => string.IsNullOrWhiteSpace(x.Name) ? "Level " + i.ToString() : x.Name).ToArray();
            double[] elevations = new double[count + 1];

            for (int i = 0; i < count; i++)
            {
                elevations[i + 1] = levelList[i].Elevation;
            }

            double[] heights = new double[count];   //Heights empty, set by elevations
            bool[] isMasterStory = new bool[count];
            isMasterStory[count - 1] = true;    //Top story as master
            string[] similarTo = new string[count];
            for (int i = 0; i < count; i++)
            {
                similarTo[i] = "";  //No similarities
            }

            bool[] spliceAbove = new bool[count];   //No splice
            double[] spliceHeight = new double[count];  //No splice

            if (m_model.Story.SetStories(names, elevations, heights, isMasterStory, similarTo, spliceAbove, spliceHeight) == 0) { }
            else
            {

                Engine.Reflection.Compute.RecordError("Failed to push levels. Levels can only be pushed to an empty model.");
            }

            return true;
        }

        /***************************************************/
    }
}
