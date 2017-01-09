using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM.Structural.Loads;
using ETABS2015;

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
            return outIds;
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
