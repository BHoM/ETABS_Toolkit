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
using BH.oM.Structure.Properties.Section;
using BH.oM.Structure.Properties.Constraint;
using BH.oM.Structure.Properties;
using BH.oM.Structure.Loads;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Common.Materials;
using BH.Engine.ETABS;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        /***************************************************/
        /**** Adapter override methods                  ****/
        /***************************************************/

        protected override bool UpdateObjects<T>(IEnumerable<T> objects)
        {
            if (typeof(T) == typeof(Node))
            {
                return UpdateObjects(objects as IEnumerable<Node>);
            }
            else
                return base.UpdateObjects<T>(objects);
        }

        /***************************************************/

        private bool UpdateObjects(IEnumerable<Node> nodes)
        {
            bool sucess = true;
            foreach (Node bhNode in nodes)
            {
                if (bhNode.Constraint != null)
                {
                    string name = bhNode.CustomData[AdapterId].ToString();

                    bool[] restraint = new bool[6];
                    restraint[0] = bhNode.Constraint.TranslationX == DOFType.Fixed;
                    restraint[1] = bhNode.Constraint.TranslationY == DOFType.Fixed;
                    restraint[2] = bhNode.Constraint.TranslationZ == DOFType.Fixed;
                    restraint[3] = bhNode.Constraint.RotationX == DOFType.Fixed;
                    restraint[4] = bhNode.Constraint.RotationY == DOFType.Fixed;
                    restraint[5] = bhNode.Constraint.RotationZ == DOFType.Fixed;

                    double[] spring = new double[6];
                    spring[0] = bhNode.Constraint.TranslationalStiffnessX;
                    spring[1] = bhNode.Constraint.TranslationalStiffnessY;
                    spring[2] = bhNode.Constraint.TranslationalStiffnessZ;
                    spring[3] = bhNode.Constraint.RotationalStiffnessX;
                    spring[4] = bhNode.Constraint.RotationalStiffnessY;
                    spring[5] = bhNode.Constraint.RotationalStiffnessZ;

                    sucess &= m_model.PointObj.SetRestraint(name, ref restraint) == 0;
                    sucess &= m_model.PointObj.SetSpring(name, ref spring) == 0;
                }
            }

            return sucess;
        }

        /***************************************************/
    }
}
