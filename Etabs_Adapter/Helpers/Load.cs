using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABS2016;
using BH.oM.Structural.Loads;
using BH.oM.Structural;
using BH.oM.Base;
using BH.oM.Structural.Elements;
using BH.oM.Geometry;

namespace BH.Adapter.ETABS
{
    public static partial class Helper
    {
        public static void SetLoadcase(cSapModel model, Loadcase loadcase)
        {
            //string name = loadcase.CustomData[AdapterId].ToString();
            string name = loadcase.Name + ":::" + loadcase.Number.ToString();
            eLoadPatternType patternType = GetLoadPatternType(loadcase.Nature);
            
            model.LoadPatterns.Add(name, patternType);
        }

        public static Loadcase GetLoadcase(cSapModel model, string id)
        {
            Loadcase bhLoadcase = new Loadcase();
            int number;
            int.TryParse(id, out number);
            string[] nameNum = id.Split(new [] { ":::"}, StringSplitOptions.None);
            bhLoadcase.Name = nameNum[0];
            int.TryParse(nameNum[1], out number);
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
            string combinationName = loadCombination.Name + ":::" + loadCombination.Number.ToString();

            model.RespCombo.Add(combinationName, 0);//0=case, 1=combo

            foreach (var factorCase in loadCombination.LoadCases)
            {
                double factor = factorCase.Item1;
                Type lcType = factorCase.Item2.GetType();
                string lcName = factorCase.Item2.Name;// Number.ToString();
                eCNameType cTypeName = eCNameType.LoadCase;

                if (lcType == typeof(Loadcase))
                    cTypeName = eCNameType.LoadCase;
                else if (lcType == typeof(LoadCombination))
                    cTypeName = eCNameType.LoadCombo;

                model.RespCombo.SetCaseList(combinationName, ref cTypeName, lcName, factor);
            }
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
            ICase currentCase;
            for (int i = 0; i < caseNames.Count(); i++)
            {
                if (caseDict.TryGetValue(caseNames[i], out currentCase))
                    combination.LoadCases.Add(new Tuple<double, ICase>(factors[i], currentCase));
            }
            
            return combination;
        }

        public static void SetLoad(cSapModel model, PointForce pointForce)
        {
            double[] pfValues = new double[] { pointForce.Force.X, pointForce.Force.Y, pointForce.Force.Z, pointForce.Moment.X, pointForce.Moment.Y, pointForce.Moment.Z };
            bool replace = false;
            int ret = 0;
            foreach (Node node in pointForce.Objects.Elements)
            {
                ret = model.PointObj.SetLoadForce(node.CustomData[AdapterId].ToString(), pointForce.Loadcase.Number.ToString(), ref pfValues, replace);
            }
        }

        public static void SetLoad(cSapModel model, BarUniformlyDistributedLoad barUniformLoad)
        {
            int ret = 0;

            foreach (Bar bar in barUniformLoad.Objects.Elements)
            {
                //force
                for (int direction = 1; direction <= 3; direction++)
                {
                    double val = direction == 1 ? barUniformLoad.Force.X : direction == 2 ? barUniformLoad.Force.Y : barUniformLoad.Force.Z * (-1); //note: etabs acts different then stated in API documentstion
                    if (val != 0)
                    {
                        ret = model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterId].ToString(), barUniformLoad.Loadcase.Number.ToString(), 1, direction + 3, 0, 1, val, val);
                    }
                }
                //moments ? does not exist in old toolkit either! 
            }
        }

        public static void SetLoad(cSapModel model, AreaUniformalyDistributedLoad areaUniformLoad)
        {
            int ret = 0;
            foreach (IAreaElement area in areaUniformLoad.Objects.Elements)
            {
                for (int direction = 1; direction <= 3; direction++)
                {
                    double val = direction == 1 ? areaUniformLoad.Pressure.X : direction == 2 ? areaUniformLoad.Pressure.Y : areaUniformLoad.Pressure.Z * (-1);
                    if (val != 0)
                    {
                        //NOTE: Replace=false has been set to allow setting x,y,z-load directions !!! this should be user controled and allowed as default
                        ret = model.AreaObj.SetLoadUniform(area.CustomData[AdapterId].ToString(), areaUniformLoad.Loadcase.Number.ToString(), val, direction + 3, false);
                    }
                }
            }

        }

        public static List<ILoad> GetLoads(cSapModel model, List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();
            string[] names = null;
            string[] loadcase = null;
            string[] Csys = null;
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

            foreach (Loadcase bhLoadcase in loadcases)
            {
                if (model.PointObj.GetLoadForce("All", ref nameCount, ref names, ref loadcase, ref step, ref Csys, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, eItemType.Group) == 0)
                {
                    for (int i = 0; i < nameCount; i++)
                    {
                        if (bhLoadcase.Name == loadcase[i])
                        {
                            Vector force = new Vector() { X = fx[i], Y = fy[i], Z = fz[i] };
                            Vector moment = new Vector() { X = mx[i], Y = my[i], Z = mz[i] };
                            bhLoads.Add(new PointForce() { Force = force, Moment = moment, Loadcase = bhLoadcase });
                        }
                    }
                }

                if (model.FrameObj.GetLoadDistributed("All", ref nameCount, ref names, ref loadcase, ref step, ref Csys, ref dir, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, eItemType.Group) == 0)
                {
                    for (int i = 0; i < nameCount; i++)
                    {
                        if (bhLoadcase.Name == loadcase[i])
                        {
                            Vector force = new Vector() { X = fx[i], Y = fy[i], Z = fz[i] };
                            Vector moment = new Vector() { X = mx[i], Y = my[i], Z = mz[i] };
                            bhLoads.Add(new BarUniformlyDistributedLoad() { Force = force, Moment = moment, Loadcase = bhLoadcase });
                        }
                    }
                }

                if (model.AreaObj.GetLoadUniform("All", ref nameCount, ref names, ref loadcase, ref Csys, ref dir, ref f, eItemType.Group) == 0)
                {
                    Dictionary<string, Vector> areaUniformDict = new Dictionary<string, Vector>();

                    for (int i = 0; i < nameCount; i++)
                    {
                        if (bhLoadcase.Name == loadcase[i])
                        {
                            if (!areaUniformDict.ContainsKey(loadcase[i]))
                                areaUniformDict.Add(loadcase[i], new Vector());
                            //only sypporting global directions (x=4,y=5,z=6)
                            //would be preferential to add one load of x,y,z instead of 1 load for each direction as Etabs does
                            if (dir[i] == 4)
                                areaUniformDict[loadcase[i]].X = f[i];
                            if (dir[i] == 5)
                                areaUniformDict[loadcase[i]].Y = f[i];
                            if (dir[i] == 6)
                                areaUniformDict[loadcase[i]].Z = f[i];
                        }
                    }

                    foreach (var kvp in areaUniformDict)
                    {
                        bhLoads.Add(new AreaUniformalyDistributedLoad() { Loadcase = bhLoadcase, Pressure = kvp.Value });
                    }
                }

            }
            return bhLoads;

        }
    }
}
