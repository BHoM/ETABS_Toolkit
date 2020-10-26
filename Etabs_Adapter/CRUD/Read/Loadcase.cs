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

using System;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Loads;
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
        /***    Read Methods                             ***/
        /***************************************************/

        private List<Loadcase> ReadLoadcase(List<string> ids = null)
        {
            int nameCount = 0;
            string[] nameArr = { };

            List<Loadcase> loadcaseList = new List<Loadcase>();
            m_model.LoadPatterns.GetNameList(ref nameCount, ref nameArr);

            ids = FilterIds(ids, nameArr);

            foreach (string id in ids)
            {
                Loadcase bhLoadcase = new Loadcase();

                eLoadPatternType type = eLoadPatternType.Other;

                if (m_model.LoadPatterns.GetLoadType(id, ref type) == 0)
                {
                    bhLoadcase.Name = id;
                    bhLoadcase.Nature = LoadPatternTypeToBHoM(type);
                }

                SetAdapterId(bhLoadcase, id);
                loadcaseList.Add(bhLoadcase);
            }

            return loadcaseList;
        }

        /***************************************************/

        private List<LoadCombination> ReadLoadCombination(List<string> ids = null)
        {
            List<LoadCombination> combinations = new List<LoadCombination>();

            //get all load cases before combinations
            Dictionary<string, Loadcase> bhomCases = ReadLoadcase().ToDictionary(x => x.Name.ToString());

            int nameCount = 0;
            string[] nameArr = { };
            m_model.RespCombo.GetNameList(ref nameCount, ref nameArr);

            ids = FilterIds(ids, nameArr);

            foreach (string id in ids)
            {
                LoadCombination combination = new LoadCombination();

                string[] caseNames = null;
                double[] factors = null;
                int caseNum = 0;
                eCNameType[] nameTypes = null;//<--TODO: maybe need to check if 1? (1=loadcombo)

                if (m_model.RespCombo.GetCaseList(id, ref caseNum, ref nameTypes, ref caseNames, ref factors) == 0)
                {
                    combination.Name = id;

                    if (caseNames != null)
                    {
                        Loadcase currentCase;

                        for (int i = 0; i < caseNames.Count(); i++)
                        {
                            if (bhomCases.TryGetValue(caseNames[i], out currentCase))
                                combination.LoadCases.Add(new Tuple<double, ICase>(factors[i], currentCase));
                        }
                    }

                    SetAdapterId(combination, id);
                    combinations.Add(combination);
                }
            }

            return combinations;
        }


        /***************************************************/
        /***    Helper Methods                           ***/
        /***************************************************/

        private LoadNature LoadPatternTypeToBHoM(eLoadPatternType loadPatternType)
        {
            switch (loadPatternType)
            {
                case eLoadPatternType.Dead:
                    return LoadNature.Dead;
                case eLoadPatternType.SuperDead:
                    return LoadNature.SuperDead;
                case eLoadPatternType.Live:
                    return LoadNature.Live;
                case eLoadPatternType.Temperature:
                    return LoadNature.Temperature;
                case eLoadPatternType.Braking:
                    return LoadNature.Accidental;
                case eLoadPatternType.Prestress:
                    return LoadNature.Prestress;
                case eLoadPatternType.Wind:
                    return LoadNature.Wind;
                case eLoadPatternType.Quake:
                    return LoadNature.Seismic;
                case eLoadPatternType.Snow:
                    return LoadNature.Snow;
                default:
                    return LoadNature.Other;

            }
        }

        /***************************************************/

    }
}

