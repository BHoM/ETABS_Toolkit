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

using System.ComponentModel;

namespace BH.oM.Adapters.ETABS
{
    [Description("How an ETABS load combination combines the results of its load cases and combinations. The values follow the ETABS API numbering.")]
    public enum CombinationType
    {
        [Description("Adds the factored results. The ETABS default and what every LoadCombination without a CombinationTypeFragment becomes.")]
        LinearAdditive = 0,
        [Description("Takes the maximum and the minimum of the factored results, for example over all ULS combinations.")]
        Envelope = 1,
        [Description("Adds the absolute values of the factored results.")]
        AbsoluteAdditive = 2,
        [Description("Square root of the sum of the squares of the factored results.")]
        SRSS = 3,
        [Description("Adds the positive and the negative ranges of the factored results separately.")]
        RangeAdditive = 4,
    }
}

