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
using BH.oM.Structure.Elements;
using BH.oM.Structure.Constraints;

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
        /**** Update Node                               ****/
        /***************************************************/
        
        private bool UpdateObjects(IEnumerable<Node> nodes)
        {
            bool success = true;
            foreach (Node bhNode in nodes)
            {
                if (bhNode.Support != null)
                {
                    string name = bhNode.CustomData[AdapterIdName].ToString();

                    bool[] restraint = new bool[6];
                    restraint[0] = bhNode.Support.TranslationX == DOFType.Fixed;
                    restraint[1] = bhNode.Support.TranslationY == DOFType.Fixed;
                    restraint[2] = bhNode.Support.TranslationZ == DOFType.Fixed;
                    restraint[3] = bhNode.Support.RotationX == DOFType.Fixed;
                    restraint[4] = bhNode.Support.RotationY == DOFType.Fixed;
                    restraint[5] = bhNode.Support.RotationZ == DOFType.Fixed;

                    double[] spring = new double[6];
                    spring[0] = bhNode.Support.TranslationalStiffnessX;
                    spring[1] = bhNode.Support.TranslationalStiffnessY;
                    spring[2] = bhNode.Support.TranslationalStiffnessZ;
                    spring[3] = bhNode.Support.RotationalStiffnessX;
                    spring[4] = bhNode.Support.RotationalStiffnessY;
                    spring[5] = bhNode.Support.RotationalStiffnessZ;

                    success &= m_model.PointObj.SetRestraint(name, ref restraint) == 0;
                    success &= m_model.PointObj.SetSpring(name, ref spring) == 0;
                }
            }

            return success;
        }

        /***************************************************/
    }
}

