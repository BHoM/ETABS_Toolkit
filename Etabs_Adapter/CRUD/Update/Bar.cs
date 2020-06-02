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
        
        private bool UpdateObjects(IEnumerable<Bar> bhBars)
        {
            int ret = 0;

            int nameCount = 0;
            string[] names = { };
            m_model.FrameObj.GetNameList(ref nameCount, ref names);

            foreach (Bar bhBar in bhBars)
            {
                object o;
                if(!bhBar.CustomData.TryGetValue(AdapterIdName, out o))
                {
                    Engine.Reflection.Compute.RecordWarning("The Bar must have an ETABS adapter id to be updated.");
                    continue;
                }

                string name = o as string;
                if (!names.Contains(name))
                {
                    Engine.Reflection.Compute.RecordWarning("The Bar must be present in ETABS to be updated.");
                    continue;
                }

#if Debug16 || Release16
                string start = "";
                string end = "";
                m_model.FrameObj.GetPoints(name, ref start, ref end);

                if (bhBar.StartNode.CustomData[AdapterIdName].ToString() != start ||
                    bhBar.EndNode.CustomData[AdapterIdName].ToString() != end)
                {
                    Engine.Reflection.Compute.RecordWarning("ETABS16 does not support Update of Bar connectivity, which means the geometry can not be updated. \n" + 
                                                            "To update the connectivity or position of a Bar, delete the existing Bar you want to update and create a new one.");
                }
#endif

                if (SetObject(bhBar))
                    ret++;

            }

#if Debug16 || Release16
            // Not a problem in 2016 since it can't move them at all.
#else
            // ETABS refuses to update the location of Bars unless a edit command for them is run.
            // Mainly a UI bug, but seem to contain other artifacts as well.
            // Does not count as a command if the movment is zero.
            m_model.SelectObj.All();
            m_model.EditGeneral.Move(1, 0, 0);
            m_model.EditGeneral.Move(-1, 0, 0);
            m_model.SelectObj.ClearSelection();
#endif


            return true;
        }

        /***************************************************/

    }
}
