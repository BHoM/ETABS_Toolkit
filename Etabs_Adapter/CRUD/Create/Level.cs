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

        private bool CreateCollection(IEnumerable<Level> levels)
        {
            int count = levels.Count();
            if (count == 0)
                return true;
            if (count == 1)
            {
                Engine.Reflection.Compute.RecordError("Need to provide at least two levels to be able to push levels to ETABS through the API.");
                return false;
            }

            //Check for any duplicate level elevations
            if (levels.GroupBy(x => x.Elevation).Any(g => g.Count() > 1))
            {
                Engine.Reflection.Compute.RecordError("Duplicate level elevations provided. All provided levels need to have a unique elevation, please check the inputs.");
                return false;
            }

            List<Level> levelList = levels.OrderBy(x => x.Elevation).ToList();

            if (levelList.Any(x => string.IsNullOrWhiteSpace(x.Name)))
                Engine.Reflection.Compute.RecordWarning("Unnamed levels have been given name according to their height index: Level 'i'");

            //remove the first name, as first level will be the base level
            string[] names = levelList.Select((x, i) => string.IsNullOrWhiteSpace(x.Name) ? "Level " + i.ToString() : x.Name).Skip(1).ToArray();
            Engine.Reflection.Compute.RecordNote("First level will be the base level and will not be given the provided name");

            double[] heights = new double[count];   //Heights empty, set by elevations

            double[] elevations;

            elevations = new double[count];
            for (int i = 0; i < count; i++)
            {
                elevations[i] = levelList[i].Elevation;
            }

            //Reduce the count for the heights etc, as the baselevel is not included in the API call
            count--;

            bool[] isMasterStory = new bool[count];
            isMasterStory[count - 1] = true;    //Top story as master

            string[] similarTo = new string[count];
            for (int i = 0; i < count; i++)
            {
                similarTo[i] = "";  //No similarities
            }

            bool[] spliceAbove = new bool[count];   //No splice
            double[] spliceHeight = new double[count];  //No splice
            int[] colour = new int[count];  //no colour


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

