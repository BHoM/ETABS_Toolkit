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

namespace BH.Adapter.ETABS
{
    public static partial class Helper
    {
        public static void SetLoadcase(cSapModel model, Loadcase loadcase)
        {
            //string name = loadcase.CustomData[AdapterId].ToString();
            string name = loadcase.Number.ToString();
            eLoadPatternType patternType = GetLoadPatternType(loadcase.Nature);

            model.LoadPatterns.Add(name, patternType);
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
            string combinationName = loadCombination.Number.ToString();
            model.RespCombo.Add(combinationName, 0);//0=case, 1=combo

            foreach (var factorCase in loadCombination.LoadCases)
            {
                double factor = factorCase.Item1;
                Type lcType = factorCase.Item2.GetType();
                string lcName = factorCase.Item2.Number.ToString();
                eCNameType cTypeName = eCNameType.LoadCase;

                if (lcType == typeof(Loadcase))
                    cTypeName = eCNameType.LoadCase;
                else if (lcType == typeof(Loadcase))
                    cTypeName = eCNameType.LoadCombo;

                model.RespCombo.SetCaseList(combinationName, ref cTypeName, lcName, factor);
                
            }
            loadCombination.LoadCases
        }

        public static void SetLoad(cSapModel model, PointForce pointForce)
        {
            double[] pfValues = new double[] { pointForce.Force.X, pointForce.Force.Y, pointForce.Force.Z, pointForce.Moment.X, pointForce.Moment.Y, pointForce.Moment.Z };
            bool replace = true;
            BHoMGroup<Node> nodes = pointForce.Objects;
            foreach (Node node in pointForce.Objects.Elements)
            {
                model.PointObj.SetLoadForce(node.CustomData[AdapterId].ToString(), pointForce.Loadcase.Number.ToString(), ref pfValues, replace);
            }
        }

        public static void SetLoad(cSapModel model, BarUniformlyDistributedLoad barUniformLoad)
        {

            foreach (Bar bar in barUniformLoad.Objects.Elements)
            {
                //force
                for (int direction = 1; direction <= 3; direction++)
                {
                    double val = direction == 1 ? barUniformLoad.Force.X : direction == 2 ? barUniformLoad.Force.Y : barUniformLoad.Force.Z;
                    if (val != 0)
                    {
                        model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterId].ToString(), barUniformLoad.Loadcase.Number.ToString(), 1, direction + 3, 0, 1, val, val);
                    }
                }
                //moment - TODO: check direction is right because I am guessing here .. API is unclear
                for (int direction = 1; direction <= 3; direction++)
                {
                    double val = direction == 1 ? barUniformLoad.Moment.X : direction == 2 ? barUniformLoad.Moment.Y : barUniformLoad.Moment.Z;
                    if (val != 0)
                    {
                        model.FrameObj.SetLoadDistributed(bar.CustomData[AdapterId].ToString(), barUniformLoad.Loadcase.Number.ToString(), 2, direction, 0, 1, val, val);
                    }
                }

            }
        }

        public static void SetLoad(cSapModel model, AreaUniformalyDistributedLoad bhLoad)
        {

        }

    }
}
