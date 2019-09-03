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
using BH.oM.Architecture.Elements;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Loads;
using BH.oM.Structure.Offsets;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;
using BH.oM.Adapters.ETABS.Elements;
#if Debug2017
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        /***************************************************/
        private bool CreateObject(Node bhNode)
        {
            string name = "";

            oM.Geometry.Point position = bhNode.Position();
            if (m_model.PointObj.AddCartesian(position.X, position.Y, position.Z, ref name) == 0)
            {
                bhNode.CustomData[AdapterId] = name;

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
            }

            return true;
        }

        /***************************************************/
    }
}