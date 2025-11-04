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

using BH.Engine.Adapter;
using BH.Engine.Adapters.ETABS;
using BH.Engine.Structure;
using BH.oM.Adapters.ETABS;
using BH.oM.Analytical.Elements;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;


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

        private bool CreateObject(RigidLink bhLink)
        {
            bool success = true;

            List<string> linkIds = new List<string>();
            List<object> guids = new List<object>();

            LinkConstraint constraint = bhLink.Constraint;//not used yet
            Node primaryNode = bhLink.PrimaryNode;
            List<Node> secondaryNodes = bhLink.SecondaryNodes;

            ETABSId multiId = new ETABSId();

            for (int i = 0; i < secondaryNodes.Count(); i++)
            {

                string name = "";
                string guid = null;

                m_model.LinkObj.AddByPoint(GetAdapterId<string>(primaryNode), GetAdapterId<string>(secondaryNodes[i]), ref name, false, constraint.DescriptionOrName());
                m_model.LinkObj.GetGUID(name, ref guid);

                linkIds.Add(name);
            }

            // Assign the Unique Name to the ETABS Element
            List<string> newLinkNames = SetUniqueName(bhLink, linkIds);

            if (newLinkNames == null) return false;

            multiId.Id = newLinkNames;
            bhLink.SetAdapterId(multiId);

            return success;
        }

        /***************************************************/

        private bool CreateObject(LinkConstraint bhLinkConstraint)
        {
            string name = bhLinkConstraint.DescriptionOrName();

            bool[] dof = new bool[6];

            for (int i = 0; i < 6; i++)
                dof[i] = true;

            bool[] fix = new bool[6];

            fix[0] = bhLinkConstraint.XtoX;
            fix[1] = bhLinkConstraint.ZtoZ;
            fix[2] = bhLinkConstraint.YtoY;
            fix[3] = bhLinkConstraint.XXtoXX;
            fix[4] = bhLinkConstraint.ZZtoZZ;
            fix[5] = bhLinkConstraint.YYtoYY;

            double[] stiff = new double[6];
            double[] damp = new double[6];

            int ret = m_model.PropLink.SetLinear(name, ref dof, ref fix, ref stiff, ref damp, 0, 0);

            if (ret != 0)
                CreateElementError("Link Constraint", name);

            return ret == 0;

        }

        /***************************************************/

        [Description("Concatenates the last 7 characters of the ETABS Element GUID and the Link Name to get the Unique Name to assign to the ETABS Element.")]
        private List<string> SetUniqueName(RigidLink bhLink, List<string> names)
        {

            int ret01, ret02;
            string guid = null;
            string tempLinkName = "";
            List<string> newLinkNames = new List<string>();

            foreach (string name in names) {

                tempLinkName = "";
                ret01 = m_model.LinkObj.GetGUID(name, ref guid);

                if (bhLink.Name == "")
                {
                    tempLinkName = guid.Substring(guid.Length - 7);
                }
                else
                {
                    tempLinkName = guid.Substring(guid.Length - 7) + "::" + bhLink.Name;
                }

                ret02 = m_model.LinkObj.ChangeName(name, tempLinkName);

                newLinkNames.Add(tempLinkName);

                if (!(ret01 == 0 && ret02 == 0)) return null;

            }

            return newLinkNames;
        }

        /***************************************************/
    }
}






