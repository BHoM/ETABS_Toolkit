/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
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
                int colour = 0;
                string guid = null;
                string notes = "";
                if (m_model.PropMaterial.GetMaterial(id, ref matType, ref colour, ref notes, ref guid) == 0)
                {
                    IMaterialFragment m = null;

                    double e = 0;
                    double v = 0;
                    double thermCo = 0;
                    double g = 0;
                    double mass = 0;
                    double weight = 0;
                    m_model.PropMaterial.GetWeightAndMass(id, ref weight, ref mass);
                    if (m_model.PropMaterial.GetMPIsotropic(id, ref e, ref v, ref thermCo, ref g) != 0)
                    {
                        double[] eArr = new double[3];
                        double[] vArr = new double[3];
                        double[] aArr = new double[3];
                        double[] gArr = new double[3];
                        if (m_model.PropMaterial.GetMPOrthotropic(id, ref eArr, ref vArr, ref aArr, ref gArr) != 0)
                        {
                            string msg = string.Format("Could not extract structural properties for material {0}, this has been replaced with a GenericIsotropicMaterial with no properties.", id);
                            Engine.Base.Compute.RecordWarning(msg);
                            m = new GenericIsotropicMaterial() { Name = id + "_replacment" };
                        }
                        else
                        {
                            m = new GenericOrthotropicMaterial()
                            {
                                Name = id,
                                YoungsModulus = eArr.ToVector(),
                                PoissonsRatio = vArr.ToVector(),
                                ShearModulus = gArr.ToVector(),
                                ThermalExpansionCoeff = aArr.ToVector(),
                                Density = mass
                            };
                        }
                    }
                    else
                    {
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

                        if (m_model.PropMaterial.GetOSteel(id, ref fy, ref fu, ref efy, ref efu, ref i1, ref i2, ref v3, ref v4, ref v5) == 0 || matType == eMatType.Steel || matType == eMatType.ColdFormed ||
                            m_model.PropMaterial.GetORebar(id, ref fy, ref fu, ref efy, ref efu, ref i1, ref i2, ref v3, ref v4, ref b1) == 0 || matType == eMatType.Rebar ||
                            m_model.PropMaterial.GetOTendon(id, ref fy, ref fu, ref i1, ref i2) == 0 || matType == eMatType.Tendon)
                        {
                            m = new Steel()
                            {
                                Name = id,
                                YoungsModulus = e,
                                PoissonsRatio = v,
                                ThermalExpansionCoeff = thermCo,
                                Density = mass,
                                YieldStress = fy,
                                UltimateStress = fu
                            };
                        }
                        else if (m_model.PropMaterial.GetOConcrete(id, ref compStr, ref b1, ref tensStr, ref i1, ref i2, ref strainAtFc, ref strainUlt, ref v3, ref v4) == 0 || matType == eMatType.Concrete)
                        {
                            m = new Concrete()
                            {
                                Name = id,
                                YoungsModulus = e,
                                PoissonsRatio = v,
                                ThermalExpansionCoeff = thermCo,
                                Density = mass,
                                CylinderStrength = compStr
                            };
                        }
                        else if (matType == eMatType.Aluminum)
                        {
                            m = new Aluminium()
                            {
                                Name = id,
                                YoungsModulus = e,
                                PoissonsRatio = v,
                                ThermalExpansionCoeff = thermCo,
                                Density = mass
                            };
                        }
                        else
                        {
                            string msg = string.Format("Could not extract structural properties for material {0}, this has been replaced with a GenericIsotropicMaterial with no properties.", id);
                            Engine.Base.Compute.RecordWarning(msg);
                            m = new GenericIsotropicMaterial() { Name = id + "_replacment" };
                        }
                    }

                    SetAdapterId(m, id);
                    materialList.Add(m);
                }
            }

            return materialList;
        }

        /***************************************************/
    }
}



