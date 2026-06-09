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

using System.Collections.Generic;
using System.Linq;
using BH.Engine.Adapter;
using BH.Engine.Structure;
using BH.oM.Structure.Springs;


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
        /**** Update PointSpringProperty                ****/
        /***************************************************/

        private bool UpdateObjects(IEnumerable<PointSpringProperty> springProperties)
        {
            bool success = true;

            int nameCount = 0;
            string[] nameArr = { };
            m_model.PropPointSpring.GetNameList(ref nameCount, ref nameArr);

            foreach (PointSpringProperty spring in springProperties)
            {
                if (!nameArr.Contains(spring.DescriptionOrName()))
                {
                    Engine.Base.Compute.RecordWarning($"Failed to update PointSpringProperty: { spring.DescriptionOrName() }, no point spring property with that name found in ETABS.");
                    continue;
                }

                // SetPointSpringProp acts as both initialiser and modifier, so creation handles the update.
                success &= CreateObject(spring);
            }

            return success;
        }

        /***************************************************/

    }
}
