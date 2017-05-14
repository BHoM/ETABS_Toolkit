using BHoM.Geometry;
using BHoM.Materials;
using BHoM.Structural.Databases;
using BHoM.Structural.Properties;
using ETABS2016;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etabs_Adapter.Base
{
    public class EtabsUtils
    {
        public const string NUM_KEY = "Etabs Number";

        public static void SetDefaultKeyData(Dictionary<string, object> data, string name)
        {
            if (data.ContainsKey(NUM_KEY))
            {
                data[NUM_KEY] = name;
            }
            else
            {
                data.Add(NUM_KEY, name);
            }
        }

        //public static eLoadPatternType GetPatternType(LoadNature type)
        //{
        //    switch (type)
        //    {
        //        case LoadNature.Dead:
        //            return eLoadPatternType.Dead;
        //        case LoadNature.Live:
        //            return eLoadPatternType.Live;
        //        case LoadNature.Temperature:
        //            return eLoadPatternType.Temperature;
        //        case LoadNature.Wind:
        //            return eLoadPatternType.Wind;
        //        case LoadNature.Seismic:
        //            return eLoadPatternType.Quake;
        //        case LoadNature.Snow:
        //            return eLoadPatternType.Snow;
        //        default:
        //            return eLoadPatternType.Other;
        //    }
        //}

        public static bool IsVertical(Curve c)
        {
            Plane p = null;
            if (c.TryGetPlane(out p))
            {

                double angle = Vector.VectorAngle(p.Normal, Vector.ZAxis());
                return angle > Math.PI / 2 - Math.PI / 48 && angle < Math.PI / 2 + Math.PI / 48;
            }
            return false;
        }

        public static void CreateMaterial(cSapModel SapModel, Material mat)
        {
            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            string name = "";
            if (SapModel.PropMaterial.GetMaterial(mat.Name, ref matType, ref colour, ref notes, ref guid) != 0)
            {
                SapModel.PropMaterial.AddMaterial(ref name, GetMaterialType(mat), "", "", "");
                SapModel.PropMaterial.ChangeName(name, mat.Name);
                SapModel.PropMaterial.SetMPIsotropic(mat.Name, mat.YoungsModulus, mat.PoissonsRatio, mat.CoeffThermalExpansion);
                SapModel.PropMaterial.SetWeightAndMass(mat.Name, 0, mat.Density);
            }
        }

        public static Material GetMaterial(cSapModel SapModel, string name)
        {
            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            //string name = "";
            if (SapModel.PropMaterial.GetMaterial(name, ref matType, ref colour, ref notes, ref guid) == 0)
            {
                double e = 0;
                double v = 0;
                double thermCo = 0;
                double g = 0;
                double mass = 0;
                double weight = 0;
                SapModel.PropMaterial.GetMPIsotropic(name, ref e, ref v, ref thermCo, ref g);
                SapModel.PropMaterial.GetWeightAndMass(name, ref weight, ref mass);
                double compStr = 0;
                double tensStr = 0;
                double v1 = 0;
                double v2 = 0;
                double v3 = 0;
                double v4 = 0;
                double v5 = 0;
                int i1 = 0;
                int i2 = 0;
                bool b1 = false;

                Material m = new Material(name, GetMaterialType(matType), e, v, thermCo, g, mass);
                if (SapModel.PropMaterial.GetOSteel(name, ref compStr, ref tensStr, ref v1, ref v2, ref i1, ref i2, ref v3, ref v4, ref v5) == 0)
                {
                    m.CompressiveYieldStrength = compStr;
                    m.TensileYieldStrength = compStr;
                }
                else if (SapModel.PropMaterial.GetOConcrete(name, ref compStr, ref b1, ref tensStr, ref i1, ref i2, ref v1, ref v2, ref v3, ref v4) == 0)
                {
                    m.CompressiveYieldStrength = compStr;
                    m.TensileYieldStrength = compStr * tensStr;
                }

                return m;
            }
            return null;
        }

        public static MaterialType GetMaterialType(eMatType m)
        {
            switch (m)
            {
                case eMatType.Concrete:
                    return MaterialType.Concrete;
                case eMatType.Steel:
                    return MaterialType.Steel;
                case eMatType.NoDesign:
                    return MaterialType.Timber;
                case eMatType.Aluminum:
                    return MaterialType.Aluminium;
                default:
                    return MaterialType.Concrete;
            }
        }

        public static eMatType GetMaterialType(Material m)
        {
            switch (m.Type)
            {
                case MaterialType.Concrete:
                    return eMatType.Concrete;
                case MaterialType.Steel:
                    return eMatType.Steel;
                case MaterialType.Timber:
                    return eMatType.NoDesign;
                case MaterialType.Aluminium:
                    return eMatType.Aluminum;
                default:
                    return eMatType.NoDesign;
            }
        }



    }
}
