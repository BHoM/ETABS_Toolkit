/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2024, the respective contributors. All rights reserved.
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
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.MaterialFragments;
#if Debug16 || Release16
using ETABS2016;
#elif Debug17 || Release17
using ETABSv17;
#else
using ETABSv1;
#endif
using BH.Engine.Adapters.ETABS;

namespace BH.Adapter.ETABS
{
#if Debug16 || Release16
    public partial class ETABS2016Adapter : BHoMAdapter
#elif Debug17 || Release17
   public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/

        private List<IMaterialFragment> ReadMaterial(List<string> ids = null)
        {
            int nameCount = 0;
            string[] names = { };
            List<IMaterialFragment> materialList = new List<IMaterialFragment>();
            m_model.PropMaterial.GetNameList(ref nameCount, ref names);

            ids = FilterIds(ids, names);

            foreach (string id in ids)
            {                    
                eMatType matType = eMatType.NoDesign;
                int symType = 0;
                int colour = 0;
                string guid = "";
                string notes = "";

                if (m_model.PropMaterial.GetMaterial(id, ref matType, ref colour, ref notes, ref guid) == 0)
                {
                    IMaterialFragment bhMaterial;

                    m_model.PropMaterial.GetTypeOAPI(id, ref matType, ref symType);

                    double e = 0;
                    double v = 0;
                    double thermCo = 0;
                    double g = 0;

                    double[] E = new double[3];
                    double[] V = new double[3];
                    double[] ThermCo = new double[3];
                    double[] G = new double[3];

                    if (symType == 0)// Isotropic
                    {
                        m_model.PropMaterial.GetMPIsotropic(id, ref e, ref v, ref thermCo, ref g);
                    }
                    else if (symType == 1) // Orthotropic
                    {
                        m_model.PropMaterial.GetMPOrthotropic(id, ref E, ref V, ref ThermCo, ref G);
                    }
                    else if (symType == 2) //Anisotropic
                    {
                        m_model.PropMaterial.GetMPAnisotropic(id, ref E, ref V, ref ThermCo, ref G);
                    }
                    else if (symType == 3) //Uniaxial
                    {
                        m_model.PropMaterial.GetMPUniaxial(id, ref e, ref thermCo);
                    }

                    double mass = 0;
                    double weight = 0;

                    m_model.PropMaterial.GetWeightAndMass(id, ref weight, ref mass);

                    double fc = 0;//compressive stress
                    double fy = 0;//yield stress
                    double fu = 0;//ultimate stress
                    double efy = 0;//expected yield stress
                    double efu = 0;//expected tensile stress
                    double fcsFactor = 0; //lightweight concrete factor lambda
                    double strainHardening = 0;//strain at hardening
                    double strainMaxF = 0;//strain at max stress
                    double strainRupture = 0;//strain at rupture
                    double strainFc = 0;//strain at f'c
                    double finalSlope = 0;
                    double frictionAngle = 0;
                    double dilationAngle = 0;
                    int i0 = 0;//stress-strain curvetype
                    int i1 = 0;//stress-strain hysteresis type
                    bool b0 = false;//is lightweight



                    switch (matType)
                    {
                        case eMatType.Steel:
                            m_model.PropMaterial.GetOSteel_1(id, ref fy, ref fu, ref efy, ref efu, ref i0, ref i1, ref strainHardening, ref strainMaxF, ref strainRupture, ref finalSlope);
                            bhMaterial = Engine.Structure.Create.Steel(id, e, v, thermCo, mass, 0, fy, fu);
                            break;
                        case eMatType.Concrete:
                            m_model.PropMaterial.GetOConcrete_1(id, ref fc, ref b0, ref fcsFactor, ref i0, ref i1, ref strainFc, ref strainRupture, ref finalSlope, ref frictionAngle, ref dilationAngle);
                            bhMaterial = Engine.Structure.Create.Concrete(id, e, v, thermCo, mass, 0, 0, fy);
                            break;
                        case eMatType.Aluminum:
                            bhMaterial = Engine.Structure.Create.Aluminium(id, e, v, thermCo, mass, 0);
                            break;
                        case eMatType.ColdFormed:
                            bhMaterial = Engine.Structure.Create.Steel(id, e, v, thermCo, mass, 0);
                            break;
                        case eMatType.Rebar:
                            m_model.PropMaterial.GetORebar_1(id, ref fy, ref fu, ref efy, ref efu, ref i0, ref i1, ref strainHardening, ref strainMaxF, ref finalSlope, ref b0);
                            bhMaterial = Engine.Structure.Create.Steel(id, e, v, thermCo, mass, 0, fy, fu);
                            break;
                        case eMatType.Tendon:
                            m_model.PropMaterial.GetOTendon_1(id, ref fy, ref fu, ref i0, ref i1, ref finalSlope);
                            bhMaterial = Engine.Structure.Create.Steel(id, e, v, thermCo, mass, 0, fy, fu);
                            break;
                        case eMatType.NoDesign:
                            switch (symType)
                            {
                                case 0:
                                    bhMaterial = new GenericIsotropicMaterial() { Name = id, YoungsModulus = e, PoissonsRatio = v, ThermalExpansionCoeff = thermCo, Density = mass };
                                    break;
                                case 1:
                                    bhMaterial = new GenericOrthotropicMaterial() { Name = id, YoungsModulus = E.ToVector(), PoissonsRatio = V.ToVector(), ThermalExpansionCoeff = ThermCo.ToVector(), Density = mass };
                                    break;
                                case 2:
                                case 3:
                                default:
                                    bhMaterial = Engine.Structure.Create.Steel(id);
                                    Engine.Base.Compute.RecordWarning("Could not extract structural properties for material " + id);
                                    break;
                            }
                            break;
                        default:
                            bhMaterial = Engine.Structure.Create.Steel(id);
                            Engine.Base.Compute.RecordWarning("Could not extract structural properties for material " + id);
                            break;
                    }

                    SetAdapterId(bhMaterial, id);

                    materialList.Add(bhMaterial);
                }
            }

            return materialList;
        }

        /***************************************************/
    }
}





