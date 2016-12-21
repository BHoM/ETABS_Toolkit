using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM.Structural.Loads;
using ETABS2015;
using BHoM.Base;

namespace Etabs_Adapter.Structural.Loads
{
    public class LoadIO
    {
        //public static GetLoadcases()
        internal static List<string> GetLoadcases(cOAPI Etabs, out List<ICase> cases)
        {
            List<string> outIds = new List<string>();
            int number = 0;
            string[] names = null;

            eLoadPatternType type = eLoadPatternType.ActiveEarthPressure;
            Etabs.SapModel.LoadPatterns.GetNameList(ref number, ref names);
            cases = new List<ICase>();

            for (int i = 0; i < number; i++)
            {
                Etabs.SapModel.LoadPatterns.GetLoadType(names[i], ref type);
                cases.Add(new Loadcase(names[i], GetPatternType(type)));
                outIds.Add(names[i]);
            }


            Etabs.SapModel.LoadCases.GetNameList(ref number, ref names);

            for (int i = 0; i < number; i++)
            {
                if (!outIds.Contains(names[i]))
                {
                    cases.Add(new LoadCombination(names[i], new List<ICase>(), new List<double>()));
                    outIds.Add(names[i]);
                }
            }

            ObjectManager<ICase> caseManager = new ObjectManager<ICase>();
            caseManager.AddRange(cases);
            Etabs.SapModel.RespCombo.GetNameList(ref number, ref names);

            string[] caseNames = null;
            double[] factors = null;
            int caseNum = 0;
            eCNameType[] nameTypes = null;

            for (int i = 0; i < number; i++)
            {
                Etabs.SapModel.RespCombo.GetCaseList(names[i], ref caseNum, ref nameTypes, ref caseNames, ref factors);
                cases.Add(new LoadCombination(names[i], caseManager.GetRange(caseNames), factors.ToList()));
                outIds.Add(names[i]);
            }
            return outIds;
        }

        public static void SetLoadcases(cOAPI Etabs, List<ICase> cases)
        {
            cases.Sort(delegate (ICase c1, ICase c2)
                {
                    return c1.CaseType.CompareTo(c2.CaseType);
                });

            foreach (ICase loadcase in cases)
            {
                switch (loadcase.CaseType)
                {
                    case CaseType.Combination:
                        SetLinearCombination(Etabs, loadcase as LoadCombination);
                        break;
                    case CaseType.Simple:
                        SetSimpleCase(Etabs, loadcase as Loadcase);
                        break;
                }
            }
        }

        internal static void SetSimpleCase(cOAPI Etabs, Loadcase loadcase)
        {
            cSapModel SapModel = Etabs.SapModel;
            if (SapModel != null && loadcase != null)
            {
                SapModel.LoadPatterns.Add(loadcase.Name, GetPatternType(loadcase.Nature), loadcase.SelfWeightMultiplier);
            }
        }

        internal static void SetLinearCombination(cOAPI Etabs, LoadCombination loadcase)
        {
            cSapModel SapModel = Etabs.SapModel;
            if (SapModel != null)
            {
                int ret = 0;
                ret = SapModel.RespCombo.Add(loadcase.Name, 0);
                for (int j = 0; j < loadcase.Loadcases.Count; j++)
                {
                    eCNameType cType = loadcase.Loadcases[j].CaseType == CaseType.Simple ? eCNameType.LoadCase : eCNameType.LoadCombo;
                    ret = SapModel.RespCombo.SetCaseList(loadcase.Name, ref cType, loadcase.Loadcases[j].Name, loadcase.LoadFactors[j]);
                }
            }
        }

        public static void GetLoads(cOAPI Etabs, Loadcase loadCase, out List<ILoad> loads)
        {
            cSapModel SapModel = Etabs.SapModel;
            loads = new List<ILoad>();
            string[] names = null;
            string[] loadcase = null;
            string[] Csys = null;
            int[] step = null;
            int[] dir = null;
            double[] result = null;
            int nameCount = 0;
            double[] fx = null;
            double[] fy = null;
            double[] fz = null;
            double[] mx = null;
            double[] my = null;
            double[] mz = null;

            if (SapModel.PointObj.GetLoadForce("All", ref nameCount, ref names, ref loadcase, ref step, ref Csys, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, eItemType.Group) == 0)
            {
                for (int i = 0; i < nameCount; i++)
                {
                    //loads.Add(new PointLoad(names[i], loadcase[i], fx[i], fy[i], fz[i], mx[i], my[i], mz[i]));
                }
            }
            if (SapModel.FrameObj.GetLoadDistributed("All", ref nameCount, ref names, ref loadcase, ref step, ref Csys, ref dir, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, eItemType.Group) == 0)
            {
                for (int i = 0; i < nameCount; i++)
                {
                    // loads.Add(new UniformLine(
                }
            }
        }


        public static void SetLoads(cOAPI Etabs, List<ILoad> loads)
        {
            cSapModel SapModel = Etabs.SapModel;
            int ret = 0;

            for (int i = 0; i < loads.Count; i++)
            {
                switch (loads[i].LoadType)
                {
                    //case LoadType.PointMass:
                    //    PointMass pM = loads[i] as PointMass;
                    //    for (int j = 0; j < pM.ObjectIds.Count; j++)
                    //    {
                    //        double[] vals1 = new double[] { pM.MassX / 9.80665, pM.MassY / 9.80665, pM.MassZ / 9.80665, 0, 0, 0 };
                    //        ret = SapModel.PointObj.SetMass(pM.ObjectIds[j].ToString(), ref vals1);
                    //    }
                    //    break;
                    case LoadType.PointForce:
                        PointForce pL = loads[i] as PointForce;
                        for (int j = 0; j < pL.Objects.Count; j++)
                        {
                            double[] vals = new double[] { pL.Force.X, pL.Force.Y, pL.Force.Z, pL.Moment.X, pL.Moment.Y, pL.Moment.Z };
                            ret = SapModel.PointObj.SetLoadForce(pL.Objects[j].Name, loads[i].Name, ref vals);
                        }
                        break;
                    case LoadType.BarUniformLoad:
                        BarUniformlyDistributedLoad uL = loads[i] as BarUniformlyDistributedLoad;
                        for (int j = 0; j < uL.Objects.Count; j++)
                        {
                            for (int direction = 1; direction <= 3; direction++)
                            {
                                double val = direction == 1 ? uL.ForceVector.X : direction == 2 ? uL.ForceVector.Y : uL.ForceVector.Z;
                                if (val != 0)
                                {
                                    ret = SapModel.FrameObj.SetLoadDistributed(uL.Objects[j].Name, loads[i].Name, 1, direction + 3, 0, 1, val, val);
                                }
                            }
                        }
                        break;
                    case LoadType.AreaUniformLoad:
                        AreaUniformalyDistributedLoad uA = loads[i] as AreaUniformalyDistributedLoad;
                        for (int j = 0; j < uA.Objects.Count; j++)
                        {
                            for (int direction = 1; direction <= 3; direction++)
                            {
                                double val = direction == 1 ? uA.Pressure.X : direction == 2 ? uA.Pressure.Y : uA.Pressure.Z;
                                if (val != 0)
                                {
                                    ret = SapModel.AreaObj.SetLoadUniform(uA.Objects[j].Name, loads[i].Name, val, direction + 3);
                                }
                            }
                        }
                        break;
                    case LoadType.BarTemperature:
                        BarTemperatureLoad tLine = loads[i] as BarTemperatureLoad;
                        for (int j = 0; j < tLine.Objects.Count; j++)
                        {
                            ret = SapModel.FrameObj.SetLoadTemperature(tLine.Objects[j].Name, loads[i].Name, 1, tLine.TemperatureChange.X);
                        }

                        break;
                    case LoadType.AreaTemperature:
                        AreaTemperatureLoad tArea = loads[i] as AreaTemperatureLoad;
                        for (int j = 0; j < tArea.Objects.Count; j++)
                        {
                            ret = SapModel.FrameObj.SetLoadTemperature(tArea.Objects[j].Name, loads[i].Name, 1, tArea.TemperatureChange);
                        }

                        break;
                }
            }
        }


        internal static eLoadPatternType GetPatternType(LoadNature type)
        {
            switch (type)
            {
                case LoadNature.Dead:
                    return eLoadPatternType.Dead;
                case LoadNature.Live:
                    return eLoadPatternType.Live;
                case LoadNature.Temperature:
                    return eLoadPatternType.Temperature;
                case LoadNature.Wind:
                    return eLoadPatternType.Wind;
                case LoadNature.Seismic:
                    return eLoadPatternType.Quake;
                case LoadNature.Snow:
                    return eLoadPatternType.Snow;
                default:
                    return eLoadPatternType.Other;
            }
        }

        internal static LoadNature GetPatternType(eLoadPatternType type)
        {
            switch (type)
            {
                case eLoadPatternType.Dead:
                    return LoadNature.Dead;
                case eLoadPatternType.Live:
                    return LoadNature.Live;
                case eLoadPatternType.Temperature:
                    return LoadNature.Temperature;
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
    }
}
