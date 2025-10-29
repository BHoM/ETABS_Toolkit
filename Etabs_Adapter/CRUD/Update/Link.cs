/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
using System.Linq;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Elements;

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
        /**** Update Rigid Links                          ****/
        /***************************************************/

        private bool UpdateObjects(IEnumerable<RigidLink> bhLinks)
        {
            bool success = true;

            int nameCount = 0;
            string[] names = { };
            m_model.LinkObj.GetNameList(ref nameCount, ref names);

            foreach (RigidLink bhLink in bhLinks)
            {
                // Get underlying ETABS names for this BHoM Link
                List<string> etabsNames = GetEtabsNamesForLink(bhLink);

                if (etabsNames == null || !etabsNames.Any())
                {
                    Engine.Base.Compute.RecordWarning("The Link must have an ETABS adapter id to be updated.");
                    continue;
                }

                // Ensure at least one of the underlying link names exists in the model
                if (!etabsNames.Any(n => names.Contains(n)))
                {
                    Engine.Base.Compute.RecordWarning("The Link must be present in ETABS to be updated.");
                    continue;
                }

                // ETABS does not support updating of link connectivity or constraints here - only group assignment
                Engine.Base.Compute.RecordWarning("The Etabs API does not allow for updating of the geometry or constraint details of links. To change these, delete and recreate the link.");

                if (!UpdateGroup(bhLink))
                    success = false;
            }

            return success;
        }

        /***************************************************/

        private List<string> GetEtabsNamesForLink(RigidLink bhLink)
        {
            if (bhLink == null)
                return new List<string>();

            // Prefer explicit ETABS fragment id if present
            var frag = bhLink.Fragments?.FirstOrDefault() as ETABSId;
            if (frag != null && frag.Id != null)
            {
                switch (frag.Id)
                {
                    case List<string> list:
                        return list;
                    case string[] arr:
                        return arr.ToList();
                    case IEnumerable<string> ies:
                        return ies.ToList();
                    case IEnumerable<object> ieo:
                        return ieo.Select(o => o?.ToString()).Where(s => s != null).ToList();
                    default:
                        return new List<string> { frag.Id.ToString() };
                }
            }

            // Fallback to AdapterId string (for single link cases)
            string singleId = bhLink.AdapterId<string>(typeof(ETABSId)) ?? GetAdapterId<string>(bhLink);
            if (!string.IsNullOrEmpty(singleId))
                return new List<string> { singleId };

            return new List<string>();
        }

        /***************************************************/

        private bool UpdateGroup(RigidLink bhLink)
        {
            return ResetGroup(bhLink) && SetGroup(bhLink);
        }

        /***************************************************/

        private bool ResetGroup(RigidLink bhLink)
        {
            List<string> names = GetEtabsNamesForLink(bhLink);

            foreach (string name in names)
            {
                /* Get the names of all groups currently assigned to the link */
                int numGroups = 0;
                string[] groupNames = null;
                m_model.LinkObj.GetGroupAssign(name, ref numGroups, ref groupNames);

                /* Remove the Link from each group in the list */
                if (groupNames != null)
                    groupNames.ToList().ForEach(groupName => m_model.LinkObj.SetGroupAssign(name, groupName, true));
            }

            return true;
        }

        /***************************************************/

    }
}
