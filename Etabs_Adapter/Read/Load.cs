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
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
#if (Debug2017)
using ETABSv17;
#else
using ETABS2016;
#endif
using BH.Engine.ETABS;
using BH.oM.Geometry;
using BH.Engine.Geometry;
using BH.Engine.Reflection;
using BH.oM.Architecture.Elements;
using BH.oM.Adapters.ETABS.Elements;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        /***************************************************/

        private List<LoadCombination> ReadLoadCombination(List<string> ids = null)
        {
            List<LoadCombination> combinations = new List<LoadCombination>();

            //get all load cases before combinations
            int number = 0;
            string[] names = null;
            m_model.LoadPatterns.GetNameList(ref number, ref names);
            Dictionary<string, ICase> caseDict = new Dictionary<string, ICase>();

            //ensure id can be split into name and number
            names = EnsureNameWithNum(names.ToList()).ToArray();

            foreach (string name in names)
                caseDict.Add(name, GetLoadcase(name));

            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                m_model.RespCombo.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            //ensure id can be split into name and number
            ids = EnsureNameWithNum(ids);

            foreach (string id in ids)
            {
                combinations.Add(GetLoadCombination(caseDict, id));
            }

            return combinations;
        }

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

            //ensure id can be split into name and number
            ids = EnsureNameWithNum(ids);

            foreach (string id in ids)
            {
                loadcaseList.Add(GetLoadcase(id));
            }

            return loadcaseList;
        }

        /***************************************************/

        private List<ILoad> ReadLoad(Type type, List<string> ids = null)
        {
            List<ILoad> loadList = new List<ILoad>();

            //get loadcases first
            List<Loadcase> loadcaseList = ReadLoadcase();

            loadList = GetLoads(loadcaseList);

            //filter the list to return only the right type - No, this is not a clever way of doing it !
            loadList = loadList.Where(x => x.GetType() == type).ToList();

            return loadList;
        }

        /***************************************************/

        private Loadcase GetLoadcase(string id)
        {
            Loadcase bhLoadcase = new Loadcase();
            int number;
            string name = "NA";
            string[] nameNum = id.Split(new[] { ":::" }, StringSplitOptions.None);
            if (nameNum.Count() > 1)
            {
                name = nameNum[0];
                int.TryParse(nameNum[1], out number);
            }
            else
            {
                int.TryParse(id, out number);
            }
            bhLoadcase.Name = name;
            bhLoadcase.Number = number;

            eLoadPatternType type = eLoadPatternType.Other;

            m_model.LoadPatterns.GetLoadType(id, ref type);
            bhLoadcase.Nature = type.ToBHoM();

            return bhLoadcase;
        }

        /***************************************************/

        private LoadCombination GetLoadCombination(Dictionary<string, ICase> caseDict, string id)
        {
            LoadCombination combination = new LoadCombination();
            int number;
            string[] nameNum = id.Split(new[] { ":::" }, StringSplitOptions.None);
            int.TryParse(nameNum[1], out number);
            combination.Number = number;
            combination.Name = nameNum[0];

            string[] caseNames = null;
            double[] factors = null;
            int caseNum = 0;
            eCNameType[] nameTypes = null;//<--TODO: maybe need to check if 1? (1=loadcombo)

            m_model.RespCombo.GetCaseList(id, ref caseNum, ref nameTypes, ref caseNames, ref factors);
            if (caseNames != null)
            {
                ICase currentCase;

                for (int i = 0; i < caseNames.Count(); i++)
                {
                    if (caseDict.TryGetValue(caseNames[i], out currentCase))
                        combination.LoadCases.Add(new Tuple<double, ICase>(factors[i], currentCase));
                }

            }
            return combination;
        }

        /***************************************************/

        private List<ILoad> GetLoads(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();
            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] myType = null;
            int[] step = null;
            int[] dir = null;
            int nameCount = 0;
            double[] fx = null;
            double[] fy = null;
            double[] fz = null;
            double[] mx = null;
            double[] my = null;
            double[] mz = null;
            double[] f = null;
            double[] rd1 = null;
            double[] rd2 = null;
            double[] dist1 = null;
            double[] dist2 = null;
            double[] val1 = null;
            double[] val2 = null;

            foreach (Loadcase bhLoadcase in loadcases)
            {
                if (m_model.PointObj.GetLoadForce("All", ref nameCount, ref names, ref loadcase, ref step, ref CSys, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, eItemType.Group) == 0)
                {
                    for (int i = 0; i < nameCount; i++)
                    {
                        if (bhLoadcase.Name == loadcase[i])
                        {
                            Node fakeNode = new Node();
                            fakeNode.CustomData[AdapterId] = names[i];
                            BHoMGroup<Node> nodeObjects = new BHoMGroup<Node>() { Elements = { fakeNode } };
                            Engine.Reflection.Compute.RecordNote("An empty node with the relevant ETABS id has been returned for point loads.");
                            Vector force = new Vector() { X = fx[i], Y = fy[i], Z = fz[i] };
                            Vector moment = new Vector() { X = mx[i], Y = my[i], Z = mz[i] };
                            bhLoads.Add(new PointLoad() { Force = force, Moment = moment, Loadcase = bhLoadcase, Objects = nodeObjects });
                        }
                    }
                }

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
                            Bar fakeBar = new Bar();
                            fakeBar.CustomData[AdapterId] = names[i];
                            BHoMGroup<Bar> barObjects = new BHoMGroup<Bar>() { Elements = { fakeBar } };
                            Engine.Reflection.Compute.RecordNote("An empty bar with the relevant ETABS id has been returned for distributed loads.");
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

                if (m_model.AreaObj.GetLoadUniform("All", ref nameCount, ref names, ref loadcase, ref CSys, ref dir, ref f, eItemType.Group) == 0)
                {
                    Dictionary<string, Vector> areaUniformDict = new Dictionary<string, Vector>();
                    Vector pressure = new Vector();
                    for (int i = 0; i < nameCount; i++)
                    {
                        if (bhLoadcase.Name == loadcase[i])
                        {
                            IAreaElement fakePanel = new Panel();
                            fakePanel.CustomData[AdapterId] = names[i];
                            BHoMGroup<IAreaElement> panelObjects = new BHoMGroup<IAreaElement>() { Elements = { fakePanel } };
                            Engine.Reflection.Compute.RecordNote("An empty panel with the relevant ETABS id has been returned for distributed loads.");

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

        private List<string> EnsureNameWithNum(List<string> ids)
        {
            List<string> nameAndNum = ids;// new List<string>();
            List<int> usedNum = new List<int>();
            List<int> unnumbered = new List<int>();
            int num;
            int high;
            int low;
            int diff;

            for (int i = 0; i < ids.Count(); i++)
            {
                string[] idArr = ids[i].Split(new[] { ":::" }, StringSplitOptions.None);
                if (idArr.Count() > 1)
                {
                    int.TryParse(idArr[1], out num);
                    usedNum.Add(num);
                }
                else
                {
                    unnumbered.Add(i);
                }
            }

            high = usedNum.Count() == 0 ? 0 : usedNum.Max();
            low = usedNum.Count() == 0 ? 0 : usedNum.Min();
            diff = usedNum.Count() - ids.Count();

            int counter = 0;
            for (int j = 0; j < unnumbered.Count(); j++)
            {
                counter = j < low ? j : counter >= high ? counter + 1 : high + 1;
                nameAndNum[unnumbered[j]] = ids[unnumbered[j]] + ":::" + counter.ToString();
            }

            return nameAndNum;
        }

        /***************************************************/

    }
}
