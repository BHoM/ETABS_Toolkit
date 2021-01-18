/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2021, the respective contributors. All rights reserved.
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

using System;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Geometry.SettingOut;
using BH.Engine.Adapters.ETABS;
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

        private List<Level> ReadLevel(List<string> ids = null)
        {
            List<Level> levellist = new List<Level>();
            int numberNames = 0;
            string[] names = null;
            m_model.Story.GetNameList(ref numberNames, ref names);

            ids = FilterIds(ids, names);

            foreach (string id in ids)
            {
                ETABSId etabsid = new ETABSId();
                etabsid.Id = id;

                double elevation = 0;
                int ret = m_model.Story.GetElevation(id, ref elevation);

                string guid = null;
                m_model.Story.GetGUID(id, ref guid);
                etabsid.PersistentId = guid;

                Level lvl = new Level() { Elevation = elevation, Name = id };

                lvl.SetAdapterId(etabsid);
                levellist.Add(lvl);
            }

            return levellist;
        }

        /***************************************************/
    }
}


