/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
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

using System.Collections.Generic;
using System.Linq;
using BH.Engine.Structure;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;

#if Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/

        private bool CreateObject(IMaterialFragment material)
        {
            bool success = true;
            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            string name = "";
            if (m_model.PropMaterial.GetMaterial(material.Name, ref matType, ref colour, ref notes, ref guid) != 0)
            {
                m_model.PropMaterial.AddMaterial(ref name, material.IMaterialType().ToCSI(), "", "", "");
                m_model.PropMaterial.ChangeName(name, material.Name);
                if (material is IIsotropic)
                {
                    IIsotropic isotropic = material as IIsotropic;
                    success &= m_model.PropMaterial.SetMPIsotropic(material.Name, isotropic.YoungsModulus, isotropic.PoissonsRatio, isotropic.ThermalExpansionCoeff) == 0;
                }
                else if (material is IOrthotropic)
                {
                    IOrthotropic orthoTropic = material as IOrthotropic;
                    double[] e = orthoTropic.YoungsModulus.ToDoubleArray();
                    double[] v = orthoTropic.PoissonsRatio.ToDoubleArray();
                    double[] a = orthoTropic.ThermalExpansionCoeff.ToDoubleArray();
                    double[] g = orthoTropic.ShearModulus.ToDoubleArray();
                    success &= m_model.PropMaterial.SetMPOrthotropic(material.Name, ref e, ref v, ref a, ref g) == 0;
                }
                success &= m_model.PropMaterial.SetWeightAndMass(material.Name, 2, material.Density) == 0;
            }
            if (!success)
                Engine.Reflection.Compute.RecordWarning($"Failed to assign material: {material.Name}, ETABS may have overwritten some properties with default values");
            return success;
        }

        /***************************************************/
    }
}

