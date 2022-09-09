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
            eMatType matType = MaterialTypeToCSI(material.IMaterialType());
            string bhName = material.DescriptionOrName();
            int color = 0;
            string guid = null;
            string notes = "";
            string name = "";
            ETABSId etabsId = new ETABSId();

            Engine.Base.Compute.RecordNote("Materials are being created in ETABS, rather than using existing materials from the ETABS database. Some parameters may be based on program defaults. It is recommended to pre-load any standard materials and ensure that the name in BHoM matches.");

            // New method for creating a material. If Region, Standard, and Grade are provided verbatim, it will pick a DB material, otherwise (as written) it will use the default of the type.
            if (m_model.PropMaterial.AddMaterial(ref name, matType, "", "", "", bhName) == 0)
            {
                etabsId.Id = name;
                SetObject(material);
            }
            // This deprecated method creates a new material based on the default of the type. Keeping it here for compatibility.
            else if (m_model.PropMaterial.SetMaterial(bhName, matType, color, notes, guid) == 0) 
            {
                etabsId.Id = bhName;
                SetObject(material);
            }
            else
            {
                CreateElementError("Material", bhName);
            }
            SetAdapterId(material, etabsId);

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

            double fc = 0;
            bool lw = IsLightweight(material); //run method to get the warning message 
            double fcsFactor = 0;
            int sstype = 0;
            int sshystype = 0;
            double strainAtFc = 0;
            double strainUltimate = 0;
            double finalSlope = 0;
            double frictionAngle = 0;
            double dilationAngle = 0;

            m_model.PropMaterial.GetOConcrete_1(
                material.DescriptionOrName(),
                ref fc,
                ref lw,
                ref fcsFactor,
                ref sstype,
                ref sshystype,
                ref strainAtFc,
                ref strainUltimate,
                ref finalSlope,
                ref frictionAngle,
                ref dilationAngle
                );

            success &= (0 == m_model.PropMaterial.SetOConcrete_1(
                material.DescriptionOrName(),
                material.CylinderStrength,
                lw,
                fcsFactor,
                sstype,
                sshystype,
                strainAtFc,
                strainUltimate,
                finalSlope,
                frictionAngle,
                dilationAngle
                ));

            return success;
        }

        /***************************************************/

        private bool SetDesignMaterial(Steel material)
        {
            bool success = true;

            double fy = 0;
            double fu = 0;
            double Efy = 0;
            double Efu = 0;
            int sstype = 0;
            int sshystype = 0;
            double strainAtHardening = 0;
            double strainAtMaxStress = 0;
            double strainAtRupture = 0;
            double finalSlope = 0;

            m_model.PropMaterial.GetOSteel_1(
                material.DescriptionOrName(),
                ref fy,
                ref fu,
                ref fy,
                ref fu,
                ref sstype,
                ref sshystype,
                ref strainAtHardening,
                ref strainAtMaxStress,
                ref strainAtRupture,
                ref finalSlope
                );

            success &= (0 == m_model.PropMaterial.SetOSteel_1(
                material.DescriptionOrName(),
                material.YieldStress,
                material.UltimateStress,
                Efy,
                Efu,
                sstype,
                sshystype,
                strainAtHardening,
                strainAtMaxStress,
                strainAtRupture,
                finalSlope
                ));

            return success;
        }

        /***************************************************/

        private bool SetDesignMaterial(IMaterialFragment material)
        {
            Engine.Base.Compute.RecordWarning($"Could not set ETABS design parameters for material: {material.DescriptionOrName()}. Please set them manually in ETABS, or load the material from ETABS database prior to push.");
            return true;
        }

        /***************************************************/

        private bool IsLightweight(Concrete material)
        {
            if (material.DescriptionOrName().Contains("LW") || material.Density < 2080)
            {
                Engine.Base.Compute.RecordWarning("This concrete appears to be a lightweight type based on the name or density. ETABS has settings which account for this in some codes, but they have not been set. User should check the material design properties.");
                return true;
            }
            return false;
        }

        /***************************************************/

    }
}



