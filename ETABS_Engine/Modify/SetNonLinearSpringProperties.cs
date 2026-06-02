/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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


using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Fragments;
using BH.oM.Base.Attributes;
using BH.oM.Structure.Springs;
using System.ComponentModel;

namespace BH.Engine.Adapters.ETABS
{
    public static partial class Modify
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Sets ETABS-specific nonlinear spring properties on a NonLinearSpring. Defaults to MultiLinearElastic and Kinematic if not called.")]
        [Input("nonLinearSpring", "The NonLinearSpring to set properties on.")]
        [Input("springType", "Elastic or plastic multilinear behaviour. Defaults to MultiLinearElastic.")]
        [Input("hysteresisType", "Hysteresis model for plastic springs. Defaults to Kinematic.")]
        [Output("nonLinearSpring", "The modified NonLinearSpring.")]
        public static NonLinearSpring SetNonLinearSpringProperties(
            this NonLinearSpring nonLinearSpring,
            NonLinearSpringType springType = NonLinearSpringType.MultiLinearElastic,
            NonLinearSpringHysteresisType hysteresisType = NonLinearSpringHysteresisType.Kinematic)
        {
            if (nonLinearSpring == null)
            {
                BH.Engine.Base.Compute.RecordError(
                    "Cannot set nonlinear spring properties on a null ForceDeformationData.");
                return null;
            }

            nonLinearSpring.Fragments.AddOrReplace(new NonLinearSpringProperties
            {
                SpringType = springType,
                SpringHysteresisType = hysteresisType
            });

            return nonLinearSpring;
        }

        /***************************************************/
    }
}

