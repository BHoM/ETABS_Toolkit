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

using BH.oM.Structure.Loads;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Geometry;
using System.Linq;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;

namespace BH.Adapter.ETABS
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static void ToCSI(this Constraint6DOF support, ref bool[] restraint, ref double[] spring)
        {
            restraint = new bool[6];
            restraint[0] = support.TranslationX == DOFType.Fixed;
            restraint[1] = support.TranslationY == DOFType.Fixed;
            restraint[2] = support.TranslationZ == DOFType.Fixed;
            restraint[3] = support.RotationX == DOFType.Fixed;
            restraint[4] = support.RotationY == DOFType.Fixed;
            restraint[5] = support.RotationZ == DOFType.Fixed;

            spring = new double[6];
            spring[0] = support.TranslationalStiffnessX;
            spring[1] = support.TranslationalStiffnessY;
            spring[2] = support.TranslationalStiffnessZ;
            spring[3] = support.RotationalStiffnessX;
            spring[4] = support.RotationalStiffnessY;
            spring[5] = support.RotationalStiffnessZ;
        }

        /***************************************************/

    }

}






