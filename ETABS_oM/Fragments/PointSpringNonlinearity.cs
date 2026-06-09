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

using BH.oM.Base;
using System.ComponentModel;

namespace BH.oM.Adapters.ETABS.Fragments
{
    [Description("ETABS-specific nonlinear behaviour settings for a point spring. Attach to a PointSpringProperty to control multilinear type and hysteresis model.")]
    public class PointSpringNonlinearity : IFragment
    {
        [Description("Defines whether the point spring is multilinear elastic or plastic.")]
        public virtual PointSpringNonlinearType SpringType { get; set; } = PointSpringNonlinearType.MultiLinearElastic;

        [Description("Hysteresis model for plastic point springs. Only used when SpringType is MultiLinearPlastic.")]
        public virtual HysteresisType SpringHysteresisType { get; set; } = HysteresisType.Kinematic;
    }
}