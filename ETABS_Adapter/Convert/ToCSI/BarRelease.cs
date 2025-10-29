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

        public static bool ToCSI(this BarRelease release, ref bool[] startRestraint, ref double[] startSpring, ref bool[] endRestraint, ref double[] endSpring)
        {
            startRestraint = new bool[6];
            startRestraint[0] = release.StartRelease.TranslationX != DOFType.Fixed;
            startRestraint[1] = release.StartRelease.TranslationZ != DOFType.Fixed;
            startRestraint[2] = release.StartRelease.TranslationY != DOFType.Fixed;
            startRestraint[3] = release.StartRelease.RotationX != DOFType.Fixed;
            startRestraint[4] = release.StartRelease.RotationZ != DOFType.Fixed;
            startRestraint[5] = release.StartRelease.RotationY != DOFType.Fixed;

            startSpring = new double[6];
            startSpring[0] = release.StartRelease.TranslationalStiffnessX;
            startSpring[1] = release.StartRelease.TranslationalStiffnessZ;
            startSpring[2] = release.StartRelease.TranslationalStiffnessY;
            startSpring[3] = release.StartRelease.RotationalStiffnessX;
            startSpring[4] = release.StartRelease.RotationalStiffnessZ;
            startSpring[5] = release.StartRelease.RotationalStiffnessY;

            endRestraint = new bool[6];
            endRestraint[0] = release.EndRelease.TranslationX != DOFType.Fixed;
            endRestraint[1] = release.EndRelease.TranslationZ != DOFType.Fixed;
            endRestraint[2] = release.EndRelease.TranslationY != DOFType.Fixed;
            endRestraint[3] = release.EndRelease.RotationX != DOFType.Fixed;
            endRestraint[4] = release.EndRelease.RotationZ != DOFType.Fixed;
            endRestraint[5] = release.EndRelease.RotationY != DOFType.Fixed;

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
            { Engine.Base.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationX for both ends"); success = false; }
            if (startReleased[1] && endReleased[1])
            { Engine.Base.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationZ for both ends"); success = false; }
            if (startReleased[2] && endReleased[2])
            { Engine.Base.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationY for both ends"); success = false; }
            if (startReleased[3] && endReleased[3])
            { Engine.Base.Compute.RecordWarning($"Unstable releases have not been set, can not release RotationX for both ends"); success = false; }
            if (startReleased[4] && endReleased[4] && (startReleased[2] || endReleased[2]))
            { Engine.Base.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationY when RotationZ is released for both ends"); success = false; }
            if (startReleased[5] && endReleased[5] && (startReleased[1] || endReleased[1]))
            { Engine.Base.Compute.RecordWarning($"Unstable releases have not been set, can not release TranslationZ when RotationY is released for both ends"); success = false; }

            return success;
        }

        /***************************************************/

    }

}






