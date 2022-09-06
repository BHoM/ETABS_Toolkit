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

using System.Collections.Generic;
using System.Linq;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using BH.Engine.Structure;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.Adapters.ETABS;
using System.ComponentModel;
using System;
using BH.Engine.Base;

#if Debug16 || Release16
using ETABS2016;
#elif Debug17 || Release17
using ETABSv17;
#else
using ETABSv1;
#endif

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
        /***    Create Methods                           ***/
        /***************************************************/

        private bool CreateObject(IMaterialFragment material)
        {
            bool success = true;
            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = null;
            string notes = "";
            string name = "";
            if (m_model.PropMaterial.GetMaterial(material.DescriptionOrName(), ref matType, ref colour, ref notes, ref guid) != 0)
            {
                m_model.PropMaterial.AddMaterial(ref name, MaterialTypeToCSI(material.IMaterialType()), "", "", material.DescriptionOrName());
                m_model.PropMaterial.ChangeName(name, material.DescriptionOrName());

                success &= SetObject(material);
            }
            if (!success)
                Engine.Base.Compute.RecordWarning($"Failed to assign material: {material.DescriptionOrName()}, ETABS may have overwritten some properties with default values");
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
                if (CheckPropertyWarning(orthoTropic, x => x.YoungsModulus) &&
                    CheckPropertyWarning(orthoTropic, x => x.PoissonsRatio) &&
                    CheckPropertyWarning(orthoTropic, x => x.ThermalExpansionCoeff) &&
                    CheckPropertyWarning(orthoTropic, x => x.ShearModulus))
                {
                    double[] e = orthoTropic.YoungsModulus.ToDoubleArray();
                    double[] v = orthoTropic.PoissonsRatio.ToDoubleArray();
                    double[] a = orthoTropic.ThermalExpansionCoeff.ToDoubleArray();
                    double[] g = orthoTropic.ShearModulus.ToDoubleArray();
                    success &= m_model.PropMaterial.SetMPOrthotropic(material.DescriptionOrName(), ref e, ref v, ref a, ref g) == 0;
                }
                else
                    success = false;
            }
            success &= m_model.PropMaterial.SetWeightAndMass(material.DescriptionOrName(), 2, material.Density) == 0;

            success &= ISetDesignMaterial(material);

            SetAdapterId(material, material.Name);

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
                    Engine.Base.Compute.RecordWarning("ETABS does not contain a definition for Timber materials, the material has been set to type 'Other' with 'Orthotropic' directional symmetry");
                    return eMatType.NoDesign;
                case MaterialType.Rebar:
                    return eMatType.Rebar;
                case MaterialType.Tendon:
                    return eMatType.Tendon;
                case MaterialType.Glass:
                    Engine.Base.Compute.RecordWarning("ETABS does not contain a definition for Glass materials, the material has been set to type 'Other'");
                    return eMatType.NoDesign;
                case MaterialType.Cable:
                    Engine.Base.Compute.RecordWarning("ETABS does not contain a definition for Cable materials, the material has been set to type 'Steel'");
                    return eMatType.Steel;
                default:
                    Engine.Base.Compute.RecordWarning("BHoM material type not found, the material has been set to type 'Other'");
                    return eMatType.NoDesign;
            }
        }

        /***************************************************/

        private bool ISetDesignMaterial(IMaterialFragment material)
        {
            return SetDesignMaterial(material as dynamic);
        }

        /***************************************************/

        private bool SetDesignMaterial(Concrete material)
        {
            bool success = true;

            Engine.Base.Compute.RecordNote("ETABS Concrete design parameters are being set, but BHoM materials do not define these quantities, check carefully");

            bool lw = IsLightweight(material);
            double fcsFactor = 1.0;
            if (lw)
                fcsFactor = 0.75;

            success &= (0 == m_model.PropMaterial.SetOConcrete_1(
                material.DescriptionOrName(),
                material.CylinderStrength,
                lw,
                fcsFactor,
                2, //Mander Stress Strain curve, program default.
                4, //Concrete hysteresis type, program default.
                0.002219, //Strain at F'c, program default.
                0.005, //Strain at ultimate, program default.
                -0.01 //Final Compression Slope, program default
                ));

            return success;
        }

        /***************************************************/

        private bool SetDesignMaterial(Steel material)
        {
            bool success = true;

            Engine.Base.Compute.RecordNote("ETABS Steel design parameters are being set, but BHoM materials do not define these quantities, check carefully");

            success &= (0 == m_model.PropMaterial.SetOSteel_1(
                material.DescriptionOrName(),
                material.YieldStress,
                material.UltimateStress,
                1.1 * material.YieldStress, //program default
                1.1 * material.UltimateStress, //program default
                1, //Simple Stress Strain, program default.
                1, //Kinematic Histeresis, program default.
                0.015, //Strain at onset of hardening, program default.
                0.11, //Strain at maximum stress, program default.
                0.17, //Strain at rupture, program default.
                -0.1 //final slope, program default.
                ));

            return success;
        }

        /***************************************************/

        private bool SetDesignMaterial(IMaterialFragment material)
        {
            Engine.Base.Compute.RecordError($"Could not set ETABS design parameters for material: {material.DescriptionOrName()}. Please set them manually.");
            return true;
        }

        /***************************************************/

        private bool IsLightweight(Concrete material)
        {
            if (material.DescriptionOrName().Contains("LW"))
                return true;
            else if (material.DescriptionOrName().Contains("NW"))
                return false;

            Engine.Base.Compute.RecordWarning("Could not determine if the concrete is lightweight based on the name; it did not contain 'LW' or 'NW'. Trying to determine based on density - check results carefully.");

            return (material.Density < 2080); //approximately 130 PCF
        }

        /***************************************************/

    }
}



