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
using System.Linq;
using BH.Engine.Base;
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Fragments;
using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.Structure.Loads;

namespace BH.Engine.Adapters.ETABS
{
    public static partial class Modify
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Sets how ETABS combines the load cases and combinations of a LoadCombination. The factors are kept.")]
        [Input("loadCombination", "The LoadCombination to set the type of.")]
        [Input("combinationType", "Linear additive (the ETABS default), envelope, absolute additive, SRSS or range additive.")]
        [Output("loadCombination", "A copy of the LoadCombination carrying a CombinationTypeFragment, which the ETABS adapter pushes as that combination type.")]
        public static LoadCombination SetCombinationType(this LoadCombination loadCombination, CombinationType combinationType)
        {
            if (loadCombination == null)
            {
                Engine.Base.Compute.RecordError("Cannot set the combination type of a null LoadCombination.");
                return null;
            }

            LoadCombination clone = loadCombination.ShallowClone();
            clone.Fragments = new FragmentSet(loadCombination.Fragments.ToList());
            clone.Fragments.AddOrReplace(new CombinationTypeFragment { CombinationType = combinationType });

            return clone;
        }

        /***************************************************/
    }
}
