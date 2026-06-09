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

        [Description("Sets ETABS-specific nonlinear behaviour on a PointSpringProperty. Defaults to MultiLinearElastic and Kinematic if not called.")]
        [Input("pointSpring", "The PointSpringProperty to set the nonlinear behaviour on.")]
        [Input("springType", "Elastic or plastic multilinear behaviour. Defaults to MultiLinearElastic.")]
        [Input("hysteresisType", "Hysteresis model for plastic point springs. Defaults to Kinematic.")]
        [Output("pointSpring", "The modified PointSpringProperty.")]
        public static PointSpringProperty SetPointSpringNonlinearity(
            this PointSpringProperty pointSpring,
            PointSpringNonlinearType springType = PointSpringNonlinearType.MultiLinearElastic,
            HysteresisType hysteresisType = HysteresisType.Kinematic)
        {
            if (pointSpring == null)
            {
                BH.Engine.Base.Compute.RecordError(
                    "Cannot set nonlinear behaviour on a null PointSpringProperty.");
                return null;
            }

            pointSpring.Fragments.AddOrReplace(new PointSpringNonlinearity
            {
                SpringType = springType,
                SpringHysteresisType = hysteresisType
            });

            return pointSpring;
        }

        /***************************************************/
    }
}
