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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Properties.Constraint;

namespace BH.Adapter.ETABS
{
    public static partial class Helper
    {
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

        //public static bool[] GetRestraint6DOF(Constraint6DOF constraint)
        //{
        //    bool[] restraint = new bool[6];
        //    restraint[0] = constraint.TranslationX == DOFType.Fixed ? true : false;
        //    restraint[1] = constraint.TranslationY == DOFType.Fixed ? true : false;
        //    restraint[2] = constraint.TranslationZ == DOFType.Fixed ? true : false;
        //    restraint[3] = constraint.RotationX == DOFType.Fixed ? true : false;
        //    restraint[4] = constraint.RotationY == DOFType.Fixed ? true : false;
        //    restraint[5] = constraint.RotationZ == DOFType.Fixed ? true : false;

        //    return restraint;
        //}

        //public static double[] GetSprings6DOF(Constraint6DOF constraint)
        //{
        //    double[] spring = new double[6];
        //    spring[0] = constraint.TranslationalStiffnessX;
        //    spring[1] = constraint.TranslationalStiffnessY;
        //    spring[2] = constraint.TranslationalStiffnessZ;
        //    spring[3] = constraint.RotationalStiffnessX;
        //    spring[4] = constraint.RotationalStiffnessY;
        //    spring[5] = constraint.RotationalStiffnessZ;

        //    return spring;
        //}

    }
}
