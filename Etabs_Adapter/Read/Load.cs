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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.Engine.ETABS;
using BH.oM.Geometry;
#if (Debug2017)
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        /***************************************************/

        private List<Loadcase> ReadLoadcase(List<string> ids = null)
        {
            int nameCount = 0;
            string[] nameArr = { };

            List<Loadcase> loadcaseList = new List<Loadcase>();

            if (ids == null)
            {
                m_model.LoadPatterns.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            foreach (string id in ids)
            {
                Loadcase bhLoadcase = new Loadcase();

                eLoadPatternType type = eLoadPatternType.Other;

                if (m_model.LoadPatterns.GetLoadType(id, ref type) == 0)
                {
                    bhLoadcase.Name = id;
                    bhLoadcase.Nature = type.ToBHoM();
                }

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

            if (ids == null)
            {
                m_model.RespCombo.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }
            
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

                    combinations.Add(combination);
                }
            }

            return combinations;
        }

        /***************************************************/

        private List<ILoad> ReadLoad(Type type, List<string> ids = null)
        {
            List<ILoad> loadList = new List<ILoad>();
            
            List<Loadcase> loadcaseList = ReadLoadcase();

            if (type == typeof(PointLoad))
                return ReadPointLoad(loadcaseList);
            else if (type == typeof(BarUniformlyDistributedLoad))
                return ReadBarLoad(loadcaseList);
            else if (type == typeof(AreaUniformlyDistributedLoad))
                return ReadAreaLoad(loadcaseList);
            else
            {
                List<ILoad> loads = new List<ILoad>();
                loads.AddRange(ReadPointLoad(loadcaseList));
                loads.AddRange(ReadBarLoad(loadcaseList));
                loads.AddRange(ReadAreaLoad(loadcaseList));
                return loads;
            }
        }

        /***************************************************/

        private List<ILoad> ReadPointLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();

            Dictionary<string, Node> bhomNodes = ReadNodes().ToDictionary(x => x.CustomData[AdapterId].ToString());

            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] step = null;
            int nameCount = 0;
            double[] fx = null;
            double[] fy = null;
            double[] fz = null;
            double[] mx = null;
            double[] my = null;
            double[] mz = null;

            foreach (Loadcase bhLoadcase in loadcases)
            {
                if (m_model.PointObj.GetLoadForce("All", ref nameCount, ref names, ref loadcase, ref step, ref CSys, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, eItemType.Group) == 0)
                {
                    for (int i = 0; i < nameCount; i++)
                    {
                        if (bhLoadcase.Name == loadcase[i])
                        {
                            PointLoad bhLoad = new PointLoad()
                            {
                                Force = new Vector() { X = fx[i], Y = fy[i], Z = fz[i] },
                                Moment = new Vector() { X = mx[i], Y = my[i], Z = mz[i] },
                                Loadcase = bhLoadcase,
                                Objects = new BHoMGroup<Node>() { Elements = { bhomNodes[names[i]] } }
                            };
                            bhLoads.Add(bhLoad);
                        }
                    }
                }
            }
            return bhLoads;

        }

        /***************************************************/

        private List<ILoad> ReadBarLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();
            
            Dictionary<string, Bar> bhomBars = ReadBars().ToDictionary(x => x.CustomData[AdapterId].ToString());

            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] myType = null;
            int[] dir = null;
            int nameCount = 0;
            double[] rd1 = null;
            double[] rd2 = null;
            double[] dist1 = null;
            double[] dist2 = null;
            double[] val1 = null;
            double[] val2 = null;

            foreach (Loadcase bhLoadcase in loadcases)
            {
                if (m_model.FrameObj.GetLoadDistributed("All", ref nameCount, ref names, ref loadcase, ref myType, ref CSys, ref dir, ref rd1, ref rd2, ref dist1, ref dist2, ref val1, ref val2, eItemType.Group) == 0)
                {
                    for (int i = 0; i < nameCount; i++)
                    {
                        if (bhLoadcase.Name == loadcase[i])
                        {
                            if (dist1[i] != 0 || rd2[i] != 1)
                            {
                                BH.Engine.Reflection.Compute.RecordWarning("Partial distributed loads are not supported");
                            }
                            double val = (val1[i] + val2[i]) / 2;
                            Vector force = new Vector();
                            switch (dir[i])
                            {
                                case 4:
                                    force.X = val;
                                    break;
                                case 5:
                                    force.Y = val;
                                    break;
                                case 6:
                                    force.Z = val;
                                    break;
                                default:
                                    BH.Engine.Reflection.Compute.RecordWarning("That load direction is not supported. Dir = " + dir[i].ToString());
                                    break;
                            }
                            BHoMGroup<Bar> barObjects = new BHoMGroup<Bar>() { Elements = { bhomBars[names[i]] } };

                            switch (myType[i])
                            {
                                case 1:
                                    bhLoads.Add(new BarUniformlyDistributedLoad() { Force = force, Loadcase = bhLoadcase, Objects = barObjects });
                                    break;
                                case 2:
                                    bhLoads.Add(new BarUniformlyDistributedLoad() { Moment = force, Loadcase = bhLoadcase, Objects = barObjects });
                                    break;
                                default:
                                    BH.Engine.Reflection.Compute.RecordWarning("Could not create the load. It's not 'MyType'. MyType = " + myType[i].ToString());
                                    break;
                            }

                        }
                    }
                }
            }
            return bhLoads;

        }

        /***************************************************/

        private List<ILoad> ReadAreaLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();

            Dictionary<string, Panel> bhomPanels = ReadPanel().ToDictionary(x => x.CustomData[AdapterId].ToString());

            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] dir = null;
            int nameCount = 0;
            double[] f = null;

            foreach (Loadcase bhLoadcase in loadcases)
            {
                if (m_model.AreaObj.GetLoadUniform("All", ref nameCount, ref names, ref loadcase, ref CSys, ref dir, ref f, eItemType.Group) == 0)
                {
                    Dictionary<string, Vector> areaUniformDict = new Dictionary<string, Vector>();
                    Vector pressure = new Vector();
                    for (int i = 0; i < nameCount; i++)
                    {
                        if (bhLoadcase.Name == loadcase[i])
                        {
                            BHoMGroup<IAreaElement> panelObjects = new BHoMGroup<IAreaElement>() { Elements = { bhomPanels[names[i]] } };

                            switch (dir[i])
                            {
                                case 4:
                                    pressure.X = f[i];
                                    break;
                                case 5:
                                    pressure.Y = f[i];
                                    break;
                                case 6:
                                    pressure.Z = f[i];
                                    break;
                                default:
                                    BH.Engine.Reflection.Compute.RecordWarning("That load direction is not supported. Dir = " + dir[i].ToString());
                                    break;
                            }

                            bhLoads.Add(new AreaUniformlyDistributedLoad() { Loadcase = bhLoadcase, Pressure = pressure, Objects = panelObjects });
                        }
                    }
                }
            }
            return bhLoads;

        }

        /***************************************************/
    }
}
