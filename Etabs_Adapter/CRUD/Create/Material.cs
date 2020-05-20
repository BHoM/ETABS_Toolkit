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
using System.ComponentModel;

#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /***    Create Methods                           ***/
        /***************************************************/

        private bool CreateObject(IMaterialFragment material)
        {
            bool success = true;
            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            string name = "";
            if (m_model.PropMaterial.GetMaterial(material.DescriptionOrName(), ref matType, ref colour, ref notes, ref guid) != 0)
            {
                m_model.PropMaterial.AddMaterial(ref name, MaterialTypeToCSI(material.IMaterialType()), "", "", "");
                m_model.PropMaterial.ChangeName(name, material.DescriptionOrName());

                success &= SetObject(material);
            }
            if (!success)
                Engine.Reflection.Compute.RecordWarning($"Failed to assign material: {material.DescriptionOrName()}, ETABS may have overwritten some properties with default values");
            return success;
        }

        /***************************************************/

        [Description("Does all the ETABS interaction which does not initiate a new object in ETABS.")]
        private bool SetObject(IMaterialFragment material)
        {
            bool success = true;

            if (material is IIsotropic)
            {
                IIsotropic isotropic = material as IIsotropic;
                success &= m_model.PropMaterial.SetMPIsotropic(material.DescriptionOrName(), isotropic.YoungsModulus, isotropic.PoissonsRatio, isotropic.ThermalExpansionCoeff) == 0;
            }
            else if (material is IOrthotropic)
            {
                IOrthotropic orthoTropic = material as IOrthotropic;
                double[] e = orthoTropic.YoungsModulus.ToDoubleArray();
                double[] v = orthoTropic.PoissonsRatio.ToDoubleArray();
                double[] a = orthoTropic.ThermalExpansionCoeff.ToDoubleArray();
                double[] g = orthoTropic.ShearModulus.ToDoubleArray();
                success &= m_model.PropMaterial.SetMPOrthotropic(material.DescriptionOrName(), ref e, ref v, ref a, ref g) == 0;
            }
            success &= m_model.PropMaterial.SetWeightAndMass(material.DescriptionOrName(), 2, material.Density) == 0;

            return success;
        }

        /***************************************************/
        /***    Helper Methods                           ***/
        /***************************************************/

        private eMatType MaterialTypeToCSI(MaterialType materialType)
        {
            switch (materialType)
            {
                case MaterialType.Aluminium:
                    return eMatType.Aluminum;
                case MaterialType.Steel:
                    return eMatType.Steel;
                case MaterialType.Concrete:
                    return eMatType.Concrete;
                case MaterialType.Timber:
                    Engine.Reflection.Compute.RecordWarning("ETABS does not contain a definition for Timber materials, the material has been set to type 'Other' with 'Orthotropic' directional symmetry");
                    return eMatType.NoDesign;
                case MaterialType.Rebar:
                    return eMatType.Rebar;
                case MaterialType.Tendon:
                    return eMatType.Tendon;
                case MaterialType.Glass:
                    Engine.Reflection.Compute.RecordWarning("ETABS does not contain a definition for Glass materials, the material has been set to type 'Other'");
                    return eMatType.NoDesign;
                case MaterialType.Cable:
                    Engine.Reflection.Compute.RecordWarning("ETABS does not contain a definition for Cable materials, the material has been set to type 'Steel'");
                    return eMatType.Steel;
                default:
                    Engine.Reflection.Compute.RecordWarning("BHoM material type not found, the material has been set to type 'Other'");
                    return eMatType.NoDesign;
            }
        }

        /***************************************************/

    }
}

