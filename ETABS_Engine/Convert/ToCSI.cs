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
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using BH.oM.Structure.Loads;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Geometry;
using System.Linq;

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
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

        public static bool ToCSI(this BarRelease release, ref bool[] startRestraint, ref double[] startSpring, ref bool[] endRestraint, ref double[] endSpring)
        {
            startRestraint = new bool[6];
            startRestraint[0] = release.StartRelease.TranslationX == DOFType.Free;
            startRestraint[1] = release.StartRelease.TranslationZ == DOFType.Free;
            startRestraint[2] = release.StartRelease.TranslationY == DOFType.Free;
            startRestraint[3] = release.StartRelease.RotationX == DOFType.Free;
            startRestraint[4] = release.StartRelease.RotationZ == DOFType.Free;
            startRestraint[5] = release.StartRelease.RotationY == DOFType.Free;

            startSpring = new double[6];
            startSpring[0] = release.StartRelease.TranslationalStiffnessX;
            startSpring[1] = release.StartRelease.TranslationalStiffnessZ;
            startSpring[2] = release.StartRelease.TranslationalStiffnessY;
            startSpring[3] = release.StartRelease.RotationalStiffnessX;
            startSpring[4] = release.StartRelease.RotationalStiffnessZ;
            startSpring[5] = release.StartRelease.RotationalStiffnessY;

            endRestraint = new bool[6];
            endRestraint[0] = release.EndRelease.TranslationX == DOFType.Free;
            endRestraint[1] = release.EndRelease.TranslationZ == DOFType.Free;
            endRestraint[2] = release.EndRelease.TranslationY == DOFType.Free;
            endRestraint[3] = release.EndRelease.RotationX == DOFType.Free;
            endRestraint[4] = release.EndRelease.RotationZ == DOFType.Free;
            endRestraint[5] = release.EndRelease.RotationY == DOFType.Free;

            endSpring = new double[6];
            endSpring[0] = release.EndRelease.TranslationalStiffnessX;
            endSpring[1] = release.EndRelease.TranslationalStiffnessZ;
            endSpring[2] = release.EndRelease.TranslationalStiffnessY;
            endSpring[3] = release.EndRelease.RotationalStiffnessX;
            endSpring[4] = release.EndRelease.RotationalStiffnessZ;
            endSpring[5] = release.EndRelease.RotationalStiffnessY;

            bool[] startReleased = startRestraint.Zip(startSpring, (x, y) => x && y == 0).ToArray();
            bool[] endReleased = endRestraint.Zip(endSpring, (x, y) => x && y == 0).ToArray();
            bool success = true;

            if (startReleased[0] && endReleased[0])
            { Engine.Reflection.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationX for both ends"); success = false; }
            if (startReleased[1] && endReleased[1])
            { Engine.Reflection.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationZ for both ends"); success = false; }
            if (startReleased[2] && endReleased[2])
            { Engine.Reflection.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationY for both ends"); success = false; }
            if (startReleased[3] && endReleased[3])
            { Engine.Reflection.Compute.RecordWarning($"Unstable releases have not been set, can not release RotationX for both ends"); success = false; }
            if (startReleased[4] && endReleased[4] && (startReleased[2] || endReleased[2]))
            { Engine.Reflection.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationY when RotationZ is released for both ends"); success = false; }
            if (startReleased[5] && endReleased[5] && (startReleased[1] || endReleased[1]))
            { Engine.Reflection.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationZ when RotationY is released for both ends"); success = false; }

            return success;
        }        
        
        /***************************************************/

        public static double[] ToDoubleArray(this Vector v)
        {
            return new double[] { v.X, v.Y, v.Z };
        }

        /***************************************************/
    }
    
}

