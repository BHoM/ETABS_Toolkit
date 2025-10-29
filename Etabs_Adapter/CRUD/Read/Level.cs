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

using System;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Spatial.SettingOut;
using BH.Engine.Adapters.ETABS;
using BH.oM.Base;


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

#if Debug16 || Release16 || Debug17 || Release17
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
#else
        private List<Level> ReadLevel(List<string> ids = null)
        {
            List<Level> levellist = new List<Level>();

            int towersNum = 0;
            string[] towerNames = null;
            m_model.Tower.GetNameList(ref towersNum, ref towerNames);

            // Prepare variables
            string tableKey = "Tower and Base Story Definitions";
            string[] fieldKeyList = null;
            string groupName = "All";
            int tableVersion = 0;
            string[] fieldsKeysIncluded = null;
            int numberRecords = 0;
            string[] tableData = null;

            // Get table data
            m_model.DatabaseTables.GetTableForDisplayArray(tableKey, ref fieldKeyList, groupName, ref tableVersion, 
                                                                 ref fieldsKeysIncluded, ref numberRecords, ref tableData);


            double baseElevation = 0;
            int numberStories = 0;
            bool[] isMasterStory = null;
            bool[] spliceAbove = null;
            double[] storyElevations = null;
            double[] storyHeights = null;
            double[] spliceHeight = null;
            string[] storyNames = null;
            string[] similarToStory = null;
            int[] color = null;

            List<string> storyNamesList;
            List<double> storyElevationsList;


            for (int i = 0; i < towersNum; i++)
            {

                m_model.Tower.SetActiveTower(towerNames[i]);

                m_model.Story.GetStories_2(ref baseElevation, ref numberStories, ref storyNames, ref storyElevations, ref storyHeights,
                            ref isMasterStory, ref similarToStory, ref spliceAbove, ref spliceHeight, ref color);

                int i_baseLevelName = fieldsKeysIncluded.ToList().IndexOf("BSName")+i*fieldsKeysIncluded.Count();
                int i_baseLevelElev = fieldsKeysIncluded.ToList().IndexOf("BSElev")+i*fieldsKeysIncluded.Count();
                string baseLevelName = tableData[i_baseLevelName];
                double baseLevelElev = Double.Parse(tableData[i_baseLevelElev]);
                
                storyNamesList = storyNames.ToList();
                storyNamesList.Insert(0, baseLevelName);
                storyNames = storyNamesList.ToArray();

                storyElevationsList = storyElevations.ToList();
                storyElevationsList.Insert(0, baseLevelElev);
                storyElevations = storyElevationsList.ToArray();

                ids = FilterIds(ids, storyNames);

                for (int j = 0; j < ids.Count; j++)
                {
                    ETABSId etabsid = new ETABSId();
                    etabsid.Id = ids[j];

                    string guid = null;
                    m_model.Story.GetGUID(ids[j], ref guid);
                    etabsid.PersistentId = guid;

                    Level lvl = new Level() { Elevation = storyElevations[j], Name = ids[j] };
                    lvl.Fragments.Add( new BH.oM.Adapters.ETABS.Elements.Tower  { Name = towerNames[i] } );

                    lvl.SetAdapterId(etabsid);
                    levellist.Add(lvl);
                }
            }
            return levellist;
        }
#endif
        /***************************************************/
    }
}






