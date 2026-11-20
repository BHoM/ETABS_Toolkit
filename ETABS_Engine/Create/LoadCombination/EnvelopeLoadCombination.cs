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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Fragments;
using BH.oM.Base.Attributes;
using BH.oM.Structure.Loads;

namespace BH.Engine.Adapters.ETABS
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates a LoadCombination that ETABS pushes as an envelope: the maximum and the minimum of its load cases and load combinations, each taken with the factor 1. \n" +
            "To envelope load combinations, for example all ULS combinations, push them in the same list as the envelope and before it, so that they exist in ETABS when the envelope is created.")]
        [Input("name", "Name of the envelope. ETABS identifies load combinations by name.")]
        [Input("number", "Number of the envelope.")]
        [Input("cases", "The load cases and load combinations to envelope.")]
        [Output("envelope", "LoadCombination with a CombinationTypeFragment set to Envelope.")]
        public static LoadCombination EnvelopeLoadCombination(string name, int number, List<ICase> cases)
        {
            List<ICase> members = cases?.Where(x => x != null).ToList() ?? new List<ICase>();
            if (members.Count == 0)
            {
                Engine.Base.Compute.RecordError("Cannot create an envelope without load cases or load combinations.");
                return null;
            }

            if (members.Count < cases.Count)
                Engine.Base.Compute.RecordWarning("Null load cases or load combinations were left out of the envelope.");

            LoadCombination envelope = new LoadCombination
            {
                Name = name,
                Number = number,
                LoadCases = members.Select(x => new Tuple<double, ICase>(1.0, x)).ToList()
            };
            envelope.Fragments.AddOrReplace(new CombinationTypeFragment { CombinationType = CombinationType.Envelope });

            return envelope;
        }

        /***************************************************/
    }
}
