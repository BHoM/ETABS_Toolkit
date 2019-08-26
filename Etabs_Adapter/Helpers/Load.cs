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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if (Debug2017)
using ETABSv17;
#else
using ETABS2016;
#endif
using BH.oM.Structure.Loads;
using BH.oM.Structure;
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Geometry;

namespace BH.Adapter.ETABS
{
    public static partial class Helper
    {
        public static void SetLoadcase(cSapModel model, Loadcase loadcase)
        {
            //string name = loadcase.CustomData[AdapterId].ToString();
            string name = CaseNameToCSI(loadcase);
            eLoadPatternType patternType = GetLoadPatternType(loadcase.Nature);
            
            model.LoadPatterns.Add(name, patternType);
        }

        public static Loadcase GetLoadcase(cSapModel model, string id)
        {
            Loadcase bhLoadcase = new Loadcase();
            int number;
            string name = "NA";
            string[] nameNum = id.Split(new [] { ":::"}, StringSplitOptions.None);
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

            model.LoadPatterns.GetLoadType(id, ref type);
            bhLoadcase.Nature = GetLoadNature(type);

            return bhLoadcase;
        }

        public static LoadNature GetLoadNature(eLoadPatternType loadPatternType)
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
        public static eLoadPatternType GetLoadPatternType(LoadNature loadNature)
        {
            eLoadPatternType loadType;
            switch (loadNature)
            {
                case LoadNature.Dead:
                    loadType = eLoadPatternType.Dead;
                    break;
                case LoadNature.SuperDead:
                    loadType = eLoadPatternType.SuperDead;
                    break;
                case LoadNature.Live:
                    loadType = eLoadPatternType.Live;
                    break;
                case LoadNature.Wind:
                    loadType = eLoadPatternType.Dead;
                    break;
                case LoadNature.Seismic:
                    loadType = eLoadPatternType.Quake;
                    break;
                case LoadNature.Temperature:
                    loadType = eLoadPatternType.Temperature;
                    break;
                case LoadNature.Snow:
                    loadType = eLoadPatternType.Snow;
                    break;
                case LoadNature.Accidental:
                    loadType = eLoadPatternType.Braking;
                    break;
                case LoadNature.Prestress:
                    loadType = eLoadPatternType.Prestress;
                    break;
                case LoadNature.Other:
                    loadType = eLoadPatternType.Other;
                    break;
                default:
                    loadType = eLoadPatternType.Other;
                    break;
            }

            return loadType;

        }

        public static void SetLoadCombination(cSapModel model, LoadCombination loadCombination)
        {
            //string combinationName = loadCombination.CustomData[AdapterId].ToString();
            string combinationName = CaseNameToCSI(loadCombination);

            model.RespCombo.Add(combinationName, 0);//0=case, 1=combo
            
            foreach (var factorCase in loadCombination.LoadCases)
            {
                double factor = factorCase.Item1;
                Type lcType = factorCase.Item2.GetType();
                string lcName = CaseNameToCSI(factorCase.Item2);// factorCase.Item2.Name;// Number.ToString();
                eCNameType cTypeName = eCNameType.LoadCase;

                if (lcType == typeof(Loadcase))
                    cTypeName = eCNameType.LoadCase;
                else if (lcType == typeof(LoadCombination))
                    cTypeName = eCNameType.LoadCombo;

                model.RespCombo.SetCaseList(combinationName, ref cTypeName, lcName, factor);
            }
        }

        public static ICase CaseNameToBHoM(String csiCaseName, ICase bhomCase) // this method not used yet. Needs a second method CaseNameToCSI() and for the relevant bits to call it.
        {
            string[] nameNum = csiCaseName.Split(new[] { ":::" }, StringSplitOptions.None);
            Int32 number = new Int32();
            int.TryParse(nameNum[1], out number);
            bhomCase.Number = number;
            bhomCase.Name = nameNum[0];

            return bhomCase;
        }

        public static String CaseNameToCSI(ICase bhomCase) // this method not used yet. Needs a second method CaseNameToCSI() and for the relevant bits to call it.
        {
            string csiCaseName = bhomCase.Name + ":::" + bhomCase.Number.ToString();

            return csiCaseName;
        }

        public static LoadCombination GetLoadCombination(cSapModel model, Dictionary<string, ICase> caseDict, string id)
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

            model.RespCombo.GetCaseList(id, ref caseNum, ref nameTypes, ref caseNames, ref factors);
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

        public static void SetLoad(cSapModel model, PointLoad pointLoad, bool replace)
        {
            double[] pfValues = new double[] { pointLoad.Force.X, pointLoad.Force.Y, pointLoad.Force.Z, pointLoad.Moment.X, pointLoad.Moment.Y, pointLoad.Moment.Z };
            int ret = 0;
            foreach (Node node in pointLoad.Objects.Elements)
            {
                string csiCaseName = CaseNameToCSI(pointLoad.Loadcase);
                ret = model.PointObj.SetLoadForce(node.CustomData[AdapterId].ToString(), csiCaseName, ref pfValues, replace);
            }
        }

        public static void SetLoad(cSapModel model, BarUniformlyDistributedLoad barUniformLoad, bool replace)
        {

            foreach (Bar bar in barUniformLoad.Objects.Elements)
            {
                for (int direction = 1; direction <= 3; direction++)
                {
                    int ret = 1;
                    double val = direction == 1 ? barUniformLoad.Force.X : direction == 2 ? barUniformLoad.Force.Y : barUniformLoad.Force.Z; //note: etabs acts different then stated in API documentstion

                    if (val != 0)
                    {
                        string csiCaseName = CaseNameToCSI(barUniformLoad.Loadcase);
                        ret = model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterId].ToString(), csiCaseName, 1, direction + 3, 0, 1, val, val, "Global", true, replace);
                    }
                }
                //moments ? does not exist in old toolkit either! 
            }
        }

        public static void SetLoad(cSapModel model, AreaUniformlyDistributedLoad areaUniformLoad, bool replace)
        {
            int ret = 0;
            string csiCaseName = CaseNameToCSI(areaUniformLoad.Loadcase);
            foreach (IAreaElement area in areaUniformLoad.Objects.Elements)
            {
                for (int direction = 1; direction <= 3; direction++)
                {
                    double val = direction == 1 ? areaUniformLoad.Pressure.X : direction == 2 ? areaUniformLoad.Pressure.Y : areaUniformLoad.Pressure.Z;
                    if (val != 0)
                    {
                        //NOTE: Replace=false has been set to allow setting x,y,z-load directions !!! this should be user controled and allowed as default
                        ret = model.AreaObj.SetLoadUniform(area.CustomData[AdapterId].ToString(), csiCaseName, val, direction + 3, replace);
                    }
                }
            }
        }

        public static void SetLoad(cSapModel model, BarVaryingDistributedLoad barLoad, bool replace)
        {
            int ret = 0;

            foreach (Bar bar in barLoad.Objects.Elements)
            {
                {
                    double val1 = barLoad.ForceA.Z; //note: etabs acts different then stated in API documentstion
                    double val2 = barLoad.ForceB.Z;
                    double dist1 = barLoad.DistanceFromA;
                    double dist2 = barLoad.DistanceFromB;
                    string csiCaseName = CaseNameToCSI(barLoad.Loadcase);
                    int direction = 6; // we're doing this for Z axis only right now.
                    ret = model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterId].ToString(), csiCaseName, 1, direction, dist1, dist2, val1, val2, "Global", false, replace);
                }
            }
        }

        public static void SetLoad(cSapModel model, GravityLoad gravityLoad, bool replace)
        {
            double selfWeightMultiplier = 0;

            model.LoadPatterns.GetSelfWTMultiplier(CaseNameToCSI(gravityLoad.Loadcase), ref selfWeightMultiplier);

            if (selfWeightMultiplier != 0)
                BH.Engine.Reflection.Compute.RecordWarning($"Loadcase {gravityLoad.Loadcase.Name} allready had a selfweight multiplier which will get overridden. Previous value: {selfWeightMultiplier}, new value: {-gravityLoad.GravityDirection.Z}");

            model.LoadPatterns.SetSelfWTMultiplier(CaseNameToCSI(gravityLoad.Loadcase), -gravityLoad.GravityDirection.Z);

            if (gravityLoad.GravityDirection.X != 0 || gravityLoad.GravityDirection.Y != 0)
                Engine.Reflection.Compute.RecordError("Etabs can only handle gravity loads in global z direction");

            BH.Engine.Reflection.Compute.RecordWarning("Etabs handles gravity loads via loadcases, why only one gravity load per loadcase can be used. THis gravity load will be applied to all objects");
        }

        public static List<ILoad> GetLoads(cSapModel model, List<Loadcase> loadcases)
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
                if (model.PointObj.GetLoadForce("All", ref nameCount, ref names, ref loadcase, ref step, ref CSys, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, eItemType.Group) == 0)
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

                if (model.FrameObj.GetLoadDistributed("All", ref nameCount, ref names, ref loadcase, ref myType, ref CSys, ref dir, ref rd1, ref rd2, ref dist1, ref dist2, ref val1, ref val2, eItemType.Group) == 0)
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

                if (model.AreaObj.GetLoadUniform("All", ref nameCount, ref names, ref loadcase, ref CSys, ref dir, ref f, eItemType.Group) == 0)
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

        public static List<string> EnsureNameWithNum(List<string> ids)
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
    }
}
