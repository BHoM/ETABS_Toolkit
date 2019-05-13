///*
// * This file is part of the Buildings and Habitats object Model (BHoM)
// * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
// *
// * Each contributor holds copyright over their respective contributions.
// * The project versioning (Git) records all such contribution source information.
// *                                           
// *                                                                              
// * The BHoM is free software: you can redistribute it and/or modify         
// * it under the terms of the GNU Lesser General Public License as published by  
// * the Free Software Foundation, either version 3.0 of the License, or          
// * (at your option) any later version.                                          
// *                                                                              
// * The BHoM is distributed in the hope that it will be useful,              
// * but WITHOUT ANY WARRANTY; without even the implied warranty of               
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
// * GNU Lesser General Public License for more details.                          
// *                                                                            
// * You should have received a copy of the GNU Lesser General Public License     
// * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
// */

//using ETABS2016;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using BH.oM.Structure.MaterialFragments;
//using BH.Engine.Structure;
//using BH.oM.Geometry;

//namespace BH.Engine.ETABS
//{
//    public static partial class Convert
//    {
//        /// <summary>
//        /// NOTE: the materialName is NOT convertable to integer as the values stored in the 'name' field on most other ETABS elements
//        /// </summary>
//        public static IStructuralMaterial GetMaterial(ModelData modelData, string materialName)
//        {
//            if (modelData.materialDict.ContainsKey(materialName))
//                return modelData.materialDict[materialName];


//            eMatType matType = eMatType.NoDesign;
//            int colour = 0;
//            string guid = "";
//            string notes = "";
//            if (modelData.model.PropMaterial.GetMaterial(materialName, ref matType, ref colour, ref notes, ref guid) == 0)
//            {
//                double e = 0;
//                double v = 0;
//                double thermCo = 0;
//                double g = 0;
//                double mass = 0;
//                double weight = 0;
//                modelData.model.PropMaterial.GetMPIsotropic(materialName, ref e, ref v, ref thermCo, ref g);
//                modelData.model.PropMaterial.GetWeightAndMass(materialName, ref weight, ref mass);
//                double compStr = 0;
//                double tensStr = 0;
//                double v1 = 0;//expected yield stress
//                double v2 = 0;//expected tensile stress
//                double v3 = 0;//strain at hardening
//                double v4 = 0;//strain at max stress
//                double v5 = 0;//strain at rupture
//                int i1 = 0;//stress-strain curvetype
//                int i2 = 0;
//                bool b1 = false;

//                IStructuralMaterial m = null;
//                //new Material(name, GetMaterialType(matType), e, v, thermCo, g, mass);
//                if (modelData.model.PropMaterial.GetOSteel(materialName, ref compStr, ref tensStr, ref v1, ref v2, ref i1, ref i2, ref v3, ref v4, ref v5) == 0)
//                {
//                    m = Engine.Structure.Create.Steel(materialName, e, v, thermCo, mass, 0 , tensStr, v1);
//                }
//                else if (modelData.model.PropMaterial.GetOConcrete(materialName, ref compStr, ref b1, ref tensStr, ref i1, ref i2, ref v1, ref v2, ref v3, ref v4) == 0)
//                {
//                    m = Structure.Create.Concrete(materialName, e, v, thermCo, mass, 0);
//                }
//                //TODO: add get methods for Tendon and Rebar
//                return m;
//            }
//            return null;

//        }

//        public static void SetMaterial(ModelData modelData, IStructuralMaterial material)
//        {

//            if (!material.IsStructural())
//            {
//                Engine.Reflection.Compute.RecordWarning("Material with name " + material.Name + " is does not contain structural properties. Please check the material");
//                return;
//            }

//            eMatType matType = eMatType.NoDesign;
//            int colour = 0;
//            string guid = "";
//            string notes = "";
//            string name = "";
//            if (modelData.model.PropMaterial.GetMaterial(material.Name, ref matType, ref colour, ref notes, ref guid) != 0)
//            {
//                modelData.model.PropMaterial.AddMaterial(ref name, GetMaterialType(material.MaterialType()), "", "", "");
//                modelData.model.PropMaterial.ChangeName(name, material.Name);
//                if (material is IIsotropic)
//                {
//                    modelData.model.PropMaterial.SetMPIsotropic(material.Name, material.YoungsModulusIsotropic(), material.PoissonsRatioIsotropic(), material.ThermalExpansionCoeffIsotropic());
//                }
//                else if (material.IsOrthotropic())
//                {
//                    double[] e = material.YoungsModulusOrthotropic().ToDoubleArray();
//                    double[] v = material.PoissonsRatioOrthotropic().ToDoubleArray();
//                    double[] a = material.ThermalExpansionCoeffOrthotropic().ToDoubleArray();
//                    double[] g = material.ShearModulusOrthotropic().ToDoubleArray();
//                    modelData.model.PropMaterial.SetMPOrthotropic(material.Name, ref e, ref v, ref a, ref g);
//                }
//                modelData.model.PropMaterial.SetWeightAndMass(material.Name, 0, material.Density);
//                modelData.materialDict.Add(material.Name, material);
//            }

//        }

//        private static double[] ToDoubleArray(this Vector v)
//        {
//            return new double[] { v.X, v.Y, v.Z };
//        }

//        private static MaterialType GetMaterialType(eMatType materialType)
//        {
//            switch (materialType)
//            {
//                case eMatType.Steel:
//                    return MaterialType.Steel;
//                case eMatType.Concrete:
//                    return MaterialType.Concrete;
//                case eMatType.NoDesign://No material of this type in BHoM !!!
//                    return MaterialType.Steel;
//                case eMatType.Aluminum:
//                    return MaterialType.Aluminium;
//                case eMatType.ColdFormed:
//                    return MaterialType.Steel;
//                case eMatType.Rebar:
//                    return MaterialType.Rebar;
//                case eMatType.Tendon:
//                    return MaterialType.Tendon;
//                case eMatType.Masonry://No material of this type in BHoM !!!
//                    return MaterialType.Concrete;
//                default:
//                    return MaterialType.Steel;
//            }
//        }

//        private static eMatType GetMaterialType(MaterialType materialType)
//        {
//            switch (materialType)
//            {
//                case MaterialType.Aluminium:
//                    return eMatType.Aluminum;
//                case MaterialType.Steel:
//                    return eMatType.Steel;
//                case MaterialType.Concrete:
//                    return eMatType.Concrete;
//                case MaterialType.Timber://no material of this type in ETABS !!! 
//                    return eMatType.Steel;
//                case MaterialType.Rebar:
//                    return eMatType.Rebar;
//                case MaterialType.Tendon:
//                    return eMatType.Tendon;
//                case MaterialType.Glass://no material of this type in ETABS !!!
//                    return eMatType.Steel;
//                case MaterialType.Cable://no material of this type in ETABS !!!
//                    return eMatType.Steel;
//                default:
//                    return eMatType.Steel;
//            }
//        }
//    }
//}
