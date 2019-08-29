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
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Loads;
using BH.oM.Structure.Constraints;
#if (Debug2017)
using ETABSv17;
#else
using ETABS2016;
#endif
using BH.oM;
using BH.oM.Structure;
using BH.oM.Structure.Elements;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Geometry;


namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        public static String ToCSI(this ICase bhomCase)
        {
            string csiCaseName = bhomCase.Name + ":::" + bhomCase.Number.ToString();

            return csiCaseName;
        }

        /***************************************************/

        public static eLoadPatternType ToCSI(this LoadNature loadNature)
        {
            eLoadPatternType loadType;
            switch (loadNature)
            {
                case LoadNature.Dead:
                    loadType = eLoadPatternType.Dead;
                    break;
                case LoadNature.SuperDead:
                    loadType = eLoadPatternType.SuperDead;
                    break;
                case LoadNature.Live:
                    loadType = eLoadPatternType.Live;
                    break;
                case LoadNature.Wind:
                    loadType = eLoadPatternType.Dead;
                    break;
                case LoadNature.Seismic:
                    loadType = eLoadPatternType.Quake;
                    break;
                case LoadNature.Temperature:
                    loadType = eLoadPatternType.Temperature;
                    break;
                case LoadNature.Snow:
                    loadType = eLoadPatternType.Snow;
                    break;
                case LoadNature.Accidental:
                    loadType = eLoadPatternType.Braking;
                    break;
                case LoadNature.Prestress:
                    loadType = eLoadPatternType.Prestress;
                    break;
                case LoadNature.Other:
                    loadType = eLoadPatternType.Other;
                    break;
                default:
                    loadType = eLoadPatternType.Other;
                    break;
            }

            return loadType;

        }

        /***************************************************/
        
        public static eMatType ToCSI(this MaterialType materialType)
        {
            switch (materialType)
            {
                case MaterialType.Aluminium:
                    return eMatType.Aluminum;
                case MaterialType.Steel:
                    return eMatType.Steel;
                case MaterialType.Concrete:
                    return eMatType.Concrete;
                case MaterialType.Timber://no material of this type in ETABS !!! 
                    return eMatType.Steel;
                case MaterialType.Rebar:
                    return eMatType.Rebar;
                case MaterialType.Tendon:
                    return eMatType.Tendon;
                case MaterialType.Glass://no material of this type in ETABS !!!
                    return eMatType.Steel;
                case MaterialType.Cable://no material of this type in ETABS !!!
                    return eMatType.Steel;
                default:
                    return eMatType.Steel;
            }
        }

        /***************************************************/

        public static void GetCSIBarRelease(this Bar bar, ref bool[] startRestraint, ref double[] startSpring, ref bool[] endRestraint, ref double[] endSpring)
        {
            BarRelease release = bar.Release;

            startRestraint = new bool[6];
            startRestraint[0] = release.StartRelease.TranslationX == DOFType.Free;
            startRestraint[1] = release.StartRelease.TranslationY == DOFType.Free;
            startRestraint[2] = release.StartRelease.TranslationZ == DOFType.Free;
            startRestraint[3] = release.StartRelease.RotationX == DOFType.Free;
            startRestraint[4] = release.StartRelease.RotationY == DOFType.Free;
            startRestraint[5] = release.StartRelease.RotationZ == DOFType.Free;

            startSpring = new double[6];
            startSpring[0] = release.StartRelease.TranslationalStiffnessX;
            startSpring[1] = release.StartRelease.TranslationalStiffnessY;
            startSpring[2] = release.StartRelease.TranslationalStiffnessZ;
            startSpring[3] = release.StartRelease.RotationalStiffnessX;
            startSpring[4] = release.StartRelease.RotationalStiffnessY;
            startSpring[5] = release.StartRelease.RotationalStiffnessZ;

            endRestraint = new bool[6];
            endRestraint[0] = release.EndRelease.TranslationX == DOFType.Free;
            endRestraint[1] = release.EndRelease.TranslationY == DOFType.Free;
            endRestraint[2] = release.EndRelease.TranslationZ == DOFType.Free;
            endRestraint[3] = release.EndRelease.RotationX == DOFType.Free;
            endRestraint[4] = release.EndRelease.RotationY == DOFType.Free;
            endRestraint[5] = release.EndRelease.RotationZ == DOFType.Free;

            endSpring = new double[6];
            endSpring[0] = release.EndRelease.TranslationalStiffnessX;
            endSpring[1] = release.EndRelease.TranslationalStiffnessY;
            endSpring[2] = release.EndRelease.TranslationalStiffnessZ;
            endSpring[3] = release.EndRelease.RotationalStiffnessX;
            endSpring[4] = release.EndRelease.RotationalStiffnessY;
            endSpring[5] = release.EndRelease.RotationalStiffnessZ;
        }        
        
        /***************************************************/

        public static double[] ToDoubleArray(this Vector v)
        {
            return new double[] { v.X, v.Y, v.Z };
        }

        /***************************************************/
    }
    
}
