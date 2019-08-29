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
            bool success = true;
            int retA = 0;
            int retB = 0;
            int retC = 0;

            string name = "";

            oM.Geometry.Point position = bhNode.Position();
            retA = m_model.PointObj.AddCartesian(position.X, position.Y, position.Z, ref name);

            bhNode.CustomData[AdapterId] = name;

            if (bhNode.Support != null)
            {
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

                retB = m_model.PointObj.SetRestraint(name, ref restraint);
                retC = m_model.PointObj.SetSpring(name, ref spring);
            }

            if (retA != 0 || retB != 0 || retC != 0)
                success = false;

            return success;
        }

        /***************************************************/
    }
}
