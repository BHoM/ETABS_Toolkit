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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
#if (Debug2017)
using ETABSv17;
#else
using ETABS2016;
#endif
using BH.Engine.ETABS;
using BH.oM.Geometry;
using BH.Engine.Geometry;
using BH.Engine.Reflection;
using BH.oM.Architecture.Elements;
using BH.oM.Adapters.ETABS.Elements;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        /***************************************************/

        private List<IMaterialFragment> ReadMaterials(List<string> ids = null)
        {
            int nameCount = 0;
            string[] names = { };
            List<IMaterialFragment> materialList = new List<IMaterialFragment>();

            if (ids == null)
            {
                m_model.PropMaterial.GetNameList(ref nameCount, ref names);
                ids = names.ToList();
            }

            foreach (string id in ids)
            {
                eMatType matType = eMatType.NoDesign;
                int colour = 0;
                string guid = "";
                string notes = "";
                if (m_model.PropMaterial.GetMaterial(id, ref matType, ref colour, ref notes, ref guid) == 0)
                {
                    double e = 0;
                    double v = 0;
                    double thermCo = 0;
                    double g = 0;
                    double mass = 0;
                    double weight = 0;
                    m_model.PropMaterial.GetMPIsotropic(id, ref e, ref v, ref thermCo, ref g);
                    m_model.PropMaterial.GetWeightAndMass(id, ref weight, ref mass);
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

                    IMaterialFragment m = null;

                    if (m_model.PropMaterial.GetOSteel(id, ref fy, ref fu, ref efy, ref efu, ref i1, ref i2, ref v3, ref v4, ref v5) == 0 || matType == eMatType.Steel || matType == eMatType.ColdFormed)
                    {
                        m = Engine.Structure.Create.Steel(id, e, v, thermCo, mass, 0, fy, fu);
                    }
                    else if (m_model.PropMaterial.GetOConcrete(id, ref compStr, ref b1, ref tensStr, ref i1, ref i2, ref strainAtFc, ref strainUlt, ref v3, ref v4) == 0 || matType == eMatType.Concrete)
                    {
                        m = Engine.Structure.Create.Concrete(id, e, v, thermCo, mass, 0);
                    }
                    else if (m_model.PropMaterial.GetORebar(id, ref fy, ref fu, ref efy, ref efu, ref i1, ref i2, ref v3, ref v4, ref b1) == 0 || matType == eMatType.Rebar)
                    {
                        m = Engine.Structure.Create.Steel(id, e, v, thermCo, mass, 0, fy, fu);
                    }
                    else if (m_model.PropMaterial.GetOTendon(id, ref fy, ref fu, ref i1, ref i2) == 0 || matType == eMatType.Tendon)
                    {
                        m = Engine.Structure.Create.Steel(id, e, v, thermCo, mass, 0, fy, fu);
                    }
                    else if (matType == eMatType.Aluminum)
                    {
                        m = Engine.Structure.Create.Aluminium(id, e, v, thermCo, mass, 0);
                    }
                    else
                    {
                        Engine.Reflection.Compute.RecordWarning("Could not extract structural properties for material " + id);
                        return null;
                    }

                    m.CustomData[AdapterId] = id;

                    materialList.Add(m);
                }
            }

            return materialList;
        }

        /***************************************************/
    }
}
