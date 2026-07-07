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
                string newName = spring.DescriptionOrName();

                // Handle a rename: the property was read/created under one ETABS name (its stored adapter id)
                // and the object's name has since changed. Rename it (and its links) in place so the update
                // targets the right property and existing node assignments follow it - ETABS updates all
                // references on ChangeName. Skip if the new name is already taken by a different property.
                string oldName = spring.AdapterId<string>(AdapterIdFragmentType, false);
                if (!string.IsNullOrEmpty(oldName) && oldName != newName
                    && nameArr.Contains(oldName) && !nameArr.Contains(newName))
                {
                    RenamePointSpringProperty(oldName, newName);
                    SetAdapterId(spring, newName);

                    // Refresh the name list so the existence check below sees the renamed property.
                    m_model.PropPointSpring.GetNameList(ref nameCount, ref nameArr);
                }

                if (!nameArr.Contains(newName))
                {
                    Engine.Base.Compute.RecordWarning($"Failed to update PointSpringProperty: { newName }, no point spring property with that name found in ETABS.");
                    continue;
                }

                // SetPointSpringProp acts as both initialiser and modifier, so creation handles the update.
                success &= CreateObject(spring);
            }

            return success;
        }

        /***************************************************/

        // Renames an existing point spring property and its per-axis nonlinear links from oldName to newName.
        // Link names follow the "<propName>_<axis>" convention used on creation, so they are renamed with the
        // same prefix; renaming them first keeps the property referencing them and avoids orphaned links.
        private void RenamePointSpringProperty(string oldName, string newName)
        {
            int nLinks = 0;
            string[] linkNames = null;
            int[] axialDirs = null;
            double[] angles = null;

            if (m_model.PropPointSpring.GetLinks(oldName, ref nLinks, ref linkNames, ref axialDirs, ref angles) == 0
                && linkNames != null)
            {
                foreach (string linkName in linkNames)
                {
                    if (string.IsNullOrEmpty(linkName) || !linkName.StartsWith(oldName + "_"))
                        continue;

                    string newLinkName = newName + linkName.Substring(oldName.Length);
                    if (m_model.PropLink.ChangeName(linkName, newLinkName) != 0)
                        Engine.Base.Compute.RecordWarning($"Failed to rename spring link '{linkName}' to '{newLinkName}'.");
                }
            }

            if (m_model.PropPointSpring.ChangeName(oldName, newName) != 0)
                Engine.Base.Compute.RecordWarning($"Failed to rename point spring property '{oldName}' to '{newName}'.");
        }

        /***************************************************/

    }
}
