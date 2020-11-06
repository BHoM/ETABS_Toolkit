/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
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
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Constraints;
using BH.Engine.Structure;
using BH.Engine.Adapters.ETABS;
#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
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

            multiId.Id = linkIds;
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
    }
}

