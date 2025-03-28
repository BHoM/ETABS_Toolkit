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
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Constraints;


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

        private List<Node> ReadNode(List<string> ids = null)
        {
            List<Node> nodeList = new List<Node>();

            int nameCount = 0;
            string[] nameArr = { };
            m_model.PointObj.GetNameList(ref nameCount, ref nameArr);

            ids = FilterIds(ids, nameArr);

            foreach (string id in ids)
            {
                ETABSId etabsIdFragment = new ETABSId();
                etabsIdFragment.Id = id;

                double x, y, z;
                x = y = z = 0;
                bool[] restraint = new bool[6];
                double[] spring = new double[6];

                m_model.PointObj.GetCoordCartesian(id, ref x, ref y, ref z);

                m_model.PointObj.GetRestraint(id, ref restraint);
                m_model.PointObj.GetSpring(id, ref spring);

                Constraint6DOF support = GetConstraint6DOF(restraint, spring);

                Node bhNode = new Node { Position = new oM.Geometry.Point() { X = x, Y = y, Z = z }, Support = support };

                //Label and story
                string label = "";
                string story = "";
                string guid = null;
                if (m_model.PointObj.GetLabelFromName(id, ref label, ref story) == 0)
                {
                    etabsIdFragment.Label = label;
                    etabsIdFragment.Story = story;
                }

                if(m_model.PointObj.GetGUID(id, ref guid) == 0)
                    etabsIdFragment.PersistentId = guid;

                bhNode.SetAdapterId(etabsIdFragment);
                nodeList.Add(bhNode);
            }


            return nodeList;
        }

        /***************************************************/

        public static Constraint6DOF GetConstraint6DOF(bool[] restraint, double[] springs)
        {
            Constraint6DOF bhConstraint = new Constraint6DOF();
            bhConstraint.TranslationX = restraint[0] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.TranslationY = restraint[1] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.TranslationZ = restraint[2] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.RotationX = restraint[3] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.RotationY = restraint[4] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.RotationZ = restraint[5] == true ? DOFType.Fixed : DOFType.Free;

            bhConstraint.TranslationalStiffnessX = springs[0];
            bhConstraint.TranslationalStiffnessY = springs[1];
            bhConstraint.TranslationalStiffnessZ = springs[2];
            bhConstraint.RotationalStiffnessX = springs[3];
            bhConstraint.RotationalStiffnessY = springs[4];
            bhConstraint.RotationalStiffnessZ = springs[5];

            return bhConstraint;
        }

        /***************************************************/
    }
}






