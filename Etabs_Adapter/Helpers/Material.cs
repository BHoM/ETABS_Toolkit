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

using ETABS2016;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Physical.Materials;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Geometry;
using BH.Engine.Structure;

namespace BH.Adapter.ETABS
{
    public static partial class Helper
    {
        /// <summary>
        /// NOTE: the materialName is NOT convertable to integer as the values stored in the 'name' field on most other ETABS elements
        /// </summary>
        public static Material GetMaterial(cSapModel model, string materialName)
        {
            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            if (model.PropMaterial.GetMaterial(materialName, ref matType, ref colour, ref notes, ref guid) == 0)
            {
                double e = 0;
                double v = 0;
                double thermCo = 0;
                double g = 0;
                double mass = 0;
                double weight = 0;
                model.PropMaterial.GetMPIsotropic(materialName, ref e, ref v, ref thermCo, ref g);
                model.PropMaterial.GetWeightAndMass(materialName, ref weight, ref mass);
                double compStr = 0;
                double tensStr = 0;
                double fy = 0;//expected yield stress
                double fu = 0;//expected tensile stress
                double efy = 0;//expected yield stress
                double efu = 0;//expected tensile stress
                double v3 = 0;//strain at hardening
                double v4 = 0;//strain at max stress
                double v5 = 0;//strain at rupture
                double strainAtFc = 0;
                double strainUlt = 0;
                int i1 = 0;//stress-strain curvetype
                int i2 = 0;



                bool b1 = false;

                Material m = null;
                //new Material(name, GetMaterialType(matType), e, v, thermCo, g, mass);
                if (model.PropMaterial.GetOSteel(materialName, ref fy, ref fu, ref efy, ref efu, ref i1, ref i2, ref v3, ref v4, ref v5) == 0 || matType == eMatType.Steel || matType == eMatType.ColdFormed)
                {
                    m = Engine.Structure.Create.SteelMaterial(materialName, e, v, thermCo, mass, 0, fy, fu);
                }
                else if (model.PropMaterial.GetOConcrete(materialName, ref compStr, ref b1, ref tensStr, ref i1, ref i2, ref strainAtFc, ref strainUlt, ref v3, ref v4) == 0 || matType == eMatType.Concrete)
                {
                    m = Engine.Structure.Create.ConcreteMaterial(materialName, e, v, thermCo, mass, 0);
                }
                else if (model.PropMaterial.GetORebar(materialName, ref fy, ref fu, ref efy, ref efu, ref i1, ref i2, ref v3, ref v4, ref b1) == 0 || matType == eMatType.Rebar)
                {
                    m = Engine.Structure.Create.SteelMaterial(materialName, e, v, thermCo, mass, 0, fy, fu);
                }
                else if (model.PropMaterial.GetOTendon(materialName, ref fy, ref fu, ref i1, ref i2) == 0 || matType == eMatType.Tendon)
                {
                    m = Engine.Structure.Create.SteelMaterial(materialName, e, v, thermCo, mass, 0, fy, fu);
                }
                else if (matType == eMatType.Aluminum)
                {
                    m = Engine.Structure.Create.AluminiumMaterial(materialName, e, v, thermCo, mass, 0);
                }
                else
                {
                    m = new Material() { Name = materialName, Density = mass };
                    Engine.Reflection.Compute.RecordWarning("Could not extract structural properties for material " + materialName);
                }

                m.CustomData[AdapterId] = materialName;

                //TODO: add get methods for Tendon and Rebar
                return m;
            }
            return null;

        }

        /***************************************************/

        public static void SetMaterial(cSapModel model, Material material)
        {

            if (!material.IsStructural())
            {
                Engine.Reflection.Compute.RecordWarning("Material with name " + material.Name + " is does not contain structural properties. Please check the material");
                return;
            }

            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            string name = "";
            if (model.PropMaterial.GetMaterial(material.Name, ref matType, ref colour, ref notes, ref guid) != 0)
            {
                model.PropMaterial.AddMaterial(ref name, GetMaterialType(material.MaterialType()), "", "", "");
                model.PropMaterial.ChangeName(name, material.Name);
                if (material.IsIsotropic())
                {
                    model.PropMaterial.SetMPIsotropic(material.Name, material.YoungsModulusIsotropic(), material.PoissonsRatioIsotropic(), material.ThermalExpansionCoeffIsotropic());
                }
                else if (material.IsOrthotropic())
                {
                    double[] e = material.YoungsModulusOrthotropic().ToDoubleArray();
                    double[] v = material.PoissonsRatioOrthotropic().ToDoubleArray();
                    double[] a = material.ThermalExpansionCoeffOrthotropic().ToDoubleArray();
                    double[] g = material.ShearModulusOrthotropic().ToDoubleArray();
                    model.PropMaterial.SetMPOrthotropic(material.Name, ref e, ref v, ref a, ref g);
                }
                model.PropMaterial.SetWeightAndMass(material.Name, 0, material.Density);
            }

        }

        /***************************************************/

        private static double[] ToDoubleArray(this Vector v)
        {
            return new double[] { v.X, v.Y, v.Z };
        }

        /***************************************************/

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
