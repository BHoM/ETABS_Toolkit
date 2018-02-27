using ETABS2016;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Common.Materials;

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        /// <summary>
        /// NOTE: the materialName is NOT convertable to integer as the values stored in the 'name' field on most other ETABS elements
        /// </summary>
        public static Material GetMaterial(ModelData modelData, string materialName)
        {
            if (modelData.materialDict.ContainsKey(materialName))
                return modelData.materialDict[materialName];


            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            if (modelData.model.PropMaterial.GetMaterial(materialName, ref matType, ref colour, ref notes, ref guid) == 0)
            {
                double e = 0;
                double v = 0;
                double thermCo = 0;
                double g = 0;
                double mass = 0;
                double weight = 0;
                modelData.model.PropMaterial.GetMPIsotropic(materialName, ref e, ref v, ref thermCo, ref g);
                modelData.model.PropMaterial.GetWeightAndMass(materialName, ref weight, ref mass);
                double compStr = 0;
                double tensStr = 0;
                double v1 = 0;//expected yield stress
                double v2 = 0;//expected tensile stress
                double v3 = 0;//strain at hardening
                double v4 = 0;//strain at max stress
                double v5 = 0;//strain at rupture
                int i1 = 0;//stress-strain curvetype
                int i2 = 0;
                bool b1 = false;

                Material m = new Material();
                m.Name = materialName;
                m.Type = GetMaterialType(matType);
                
                m.PoissonsRatio = v;
                m.ShearModulus = g;
                m.YoungsModulus = e;
                m.CoeffThermalExpansion = thermCo;
                m.Density = mass;
                //new Material(name, GetMaterialType(matType), e, v, thermCo, g, mass);
                if (modelData.model.PropMaterial.GetOSteel(materialName, ref compStr, ref tensStr, ref v1, ref v2, ref i1, ref i2, ref v3, ref v4, ref v5) == 0)
                {
                    m.CompressiveYieldStrength = compStr;
                    m.TensileYieldStrength = compStr;
                }
                else if (modelData.model.PropMaterial.GetOConcrete(materialName, ref compStr, ref b1, ref tensStr, ref i1, ref i2, ref v1, ref v2, ref v3, ref v4) == 0)
                {
                    m.CompressiveYieldStrength = compStr;
                    m.TensileYieldStrength = compStr * tensStr;
                }
                //TODO: add get methods for Tendon and Rebar
                return m;
            }
            return null;

        }

        public static void SetMaterial(ModelData modelData, Material material)
        {
            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            string name = "";
            if (modelData.model.PropMaterial.GetMaterial(material.Name, ref matType, ref colour, ref notes, ref guid) != 0)
            {
                modelData.model.PropMaterial.AddMaterial(ref name, GetMaterialType(material.Type), "", "", "");
                modelData.model.PropMaterial.ChangeName(name, material.Name);
                modelData.model.PropMaterial.SetMPIsotropic(material.Name, material.YoungsModulus, material.PoissonsRatio, material.CoeffThermalExpansion);
                modelData.model.PropMaterial.SetWeightAndMass(material.Name, 0, material.Density);
                modelData.materialDict.Add(material.Name, material);
            }

        }

        private static MaterialType GetMaterialType(eMatType materialType)
        {
            switch (materialType)
            {
                case eMatType.Steel:
                    return MaterialType.Steel;
                case eMatType.Concrete:
                    return MaterialType.Concrete;
                case eMatType.NoDesign://No material of this type in BHoM !!!
                    return MaterialType.Steel;
                case eMatType.Aluminum:
                    return MaterialType.Aluminium;
                case eMatType.ColdFormed:
                    return MaterialType.Steel;
                case eMatType.Rebar:
                    return MaterialType.Rebar;
                case eMatType.Tendon:
                    return MaterialType.Tendon;
                case eMatType.Masonry://No material of this type in BHoM !!!
                    return MaterialType.Concrete;
                default:
                    return MaterialType.Steel;
            }
        }

        private static eMatType GetMaterialType(MaterialType materialType)
        {
            switch (materialType)
            {
                case MaterialType.Aluminium:
                    return eMatType.Aluminum;
                case MaterialType.Steel:
                    return eMatType.Steel;
                case MaterialType.Concrete:
                    return eMatType.Concrete;
                case MaterialType.Timber://no material of this type in ETABS !!! 
                    return eMatType.Steel;
                case MaterialType.Rebar:
                    return eMatType.Rebar;
                case MaterialType.Tendon:
                    return eMatType.Tendon;
                case MaterialType.Glass://no material of this type in ETABS !!!
                    return eMatType.Steel;
                case MaterialType.Cable://no material of this type in ETABS !!!
                    return eMatType.Steel;
                default:
                    return eMatType.Steel;
            }
        }
    }
}
