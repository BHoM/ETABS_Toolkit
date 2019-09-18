/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
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
using BH.oM.Structure.Elements;
using BH.oM.Structure.Constraints;
#if Debug2017
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug2017
    public partial class ETABS2017Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/

        private bool CreateObject(RigidLink bhLink)
        {
            bool success = true;
            int retA = 0;

            List<string> linkIds = null;

            LinkConstraint constraint = bhLink.Constraint;//not used yet
            Node masterNode = bhLink.MasterNode;
            List<Node> slaveNodes = bhLink.SlaveNodes;
            bool multiSlave = slaveNodes.Count() == 1 ? false : true;

            for (int i = 0; i < slaveNodes.Count(); i++)
            {
                string name = "";

                retA = m_model.LinkObj.AddByPoint(masterNode.CustomData[AdapterId].ToString(), slaveNodes[i].CustomData[AdapterId].ToString(), ref name, false, constraint.Name);

                linkIds.Add(name);
            }

            bhLink.CustomData[AdapterId] = linkIds;

            return success;
        }

        /***************************************************/

        private bool CreateObject(LinkConstraint bhLinkConstraint)
        {

            string name = bhLinkConstraint.Name;

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

        public override string ToString()
        {
#if Debug2017
    return base.ToString();
#else
            return base.ToString();
#endif

        }

        /***************************************************/
    }
}
