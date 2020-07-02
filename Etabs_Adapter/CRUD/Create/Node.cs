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
using BH.Engine.Structure;
using BH.Engine.Adapters.ETABS;
using BH.Engine.Geometry;
using BH.oM.Geometry;

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
        private bool CreateObject(Node bhNode)
        {
            string name = "";

            oM.Geometry.Point position = bhNode.Position;
            if (m_model.PointObj.AddCartesian(position.X, position.Y, position.Z, ref name) == 0)
            {
                bhNode.CustomData[AdapterIdName] = name;

                SetObject(bhNode, name);
            }

            return true;
        }

        /***************************************************/

        private bool SetObject(Node bhNode, string name)
        {
            if (bhNode.Support != null)
            {
                bool[] restraint = new bool[6];
                double[] spring = new double[6];

                bhNode.Support.ToCSI(ref restraint, ref spring);

                if (m_model.PointObj.SetRestraint(name, ref restraint) == 0) { }
                else
                {
                    CreatePropertyWarning("Node Restraint", "Node", name);
                }
                if (m_model.PointObj.SetSpring(name, ref spring) == 0) { }
                else
                {
                    CreatePropertyWarning("Node Spring", "Node", name);
                }
            }

            if (bhNode.Orientation != null && !bhNode.Orientation.IsEqual(Basis.XY))
            {
                Engine.Reflection.Compute.RecordWarning("ETABS does not support local coordinate systems other than the global one. Any nodes pushed will have been so as if they had the global coordinatesystem.");
            }

            return true;
        }

        /***************************************************/
    }
}

