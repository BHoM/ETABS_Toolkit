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
using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.Constraints;
using BH.Adapter.ETABS;

#if (Debug2017)
using ETABSv17;
#else
using ETABS2016;
#endif

// ******************************************************
// NOTE
// These Engine methods are improperly put in the Adapter Project
// as a temporary workaround to the different naming of ETABS dlls (2016, 2017).
// Any Engine method that does not require a direct reference to the ETABS dlls
// must be put in the Engine project.
// ******************************************************

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        /***************************************************/

        public static List<Node> ToBHoM(this cPointObj pointObj, List<string> ids)
        {
            List<Node> bhNodes = new List<Node>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                pointObj.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            foreach (string id in ids)
            {
                bhNodes.Add(pointObj.ToBHoM(id));
            }

            return bhNodes;
        }

        /***************************************************/

        public static Node ToBHoM(this cPointObj pointObj, string id)
        {

            double x, y, z;
            x = y = z = 0;
            bool[] restraint = new bool[6];
            double[] spring = new double[6];

            pointObj.GetCoordCartesian(id, ref x, ref y, ref z);

            pointObj.GetRestraint(id, ref restraint);
            pointObj.SetSpring(id, ref spring);

            Node bhNode = Structure.Create.Node(new oM.Geometry.Point() { X = x, Y = y, Z = z }, "", GetConstraint6DOF(restraint, spring));
            bhNode.CustomData.Add(ETABS2017Adapter.ID, id);

            return bhNode;
        }

        /***************************************************/

        public static LoadNature ToBHoM(this eLoadPatternType loadPatternType)
        {
            switch (loadPatternType)
            {
                case eLoadPatternType.Dead:
                    return LoadNature.Dead;
                case eLoadPatternType.SuperDead:
                    return LoadNature.SuperDead;
                case eLoadPatternType.Live:
                    return LoadNature.Live;
                case eLoadPatternType.Temperature:
                    return LoadNature.Temperature;
                case eLoadPatternType.Braking:
                    return LoadNature.Accidental;
                case eLoadPatternType.Prestress:
                    return LoadNature.Prestress;
                case eLoadPatternType.Wind:
                    return LoadNature.Wind;
                case eLoadPatternType.Quake:
                    return LoadNature.Seismic;
                case eLoadPatternType.Snow:
                    return LoadNature.Snow;
                default:
                    return LoadNature.Other;

            }
        }       
        
        /***************************************************/

        public static MaterialType ToBHoM(this eMatType materialType)
        {
            switch (materialType)
            {
                case eMatType.Steel:
                    return MaterialType.Steel;
                case eMatType.Concrete:
                    return MaterialType.Concrete;
                case eMatType.NoDesign://No material of this type in BHoM !!!
                    return MaterialType.Steel;
                case eMatType.Aluminum:
                    return MaterialType.Aluminium;
                case eMatType.ColdFormed:
                    return MaterialType.Steel;
                case eMatType.Rebar:
                    return MaterialType.Rebar;
                case eMatType.Tendon:
                    return MaterialType.Tendon;
                case eMatType.Masonry://No material of this type in BHoM !!!
                    return MaterialType.Concrete;
                default:
                    return MaterialType.Steel;
            }
        }

        /***************************************************/

        public static String ToBHoM(this eShellType shellType)
        {
            switch (shellType)
            {
                case eShellType.ShellThin:
                    return oM.Adapters.ETABS.ShellType.ShellThin.ToString();
                case eShellType.ShellThick:
                    return oM.Adapters.ETABS.ShellType.ShellThick.ToString();
                case eShellType.Membrane:
                    return oM.Adapters.ETABS.ShellType.Membrane.ToString();
                default:
                    return "None";
            }
        }

        /***************************************************/

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

        /***************************************************/

        public static BarRelease GetBarRelease(bool[] startRestraint, double[] startSpring, bool[] endRestraint, double[] endSpring)
        {
            Constraint6DOF startRelease = new Constraint6DOF();

            startRelease.TranslationX = startRestraint[0] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.TranslationY = startRestraint[1] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.TranslationZ = startRestraint[2] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.RotationX = startRestraint[3] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.RotationY = startRestraint[4] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.RotationZ = startRestraint[5] == true ? DOFType.Free : DOFType.Fixed;

            startRelease.TranslationalStiffnessX = startSpring[0];
            startRelease.TranslationalStiffnessY = startSpring[1];
            startRelease.TranslationalStiffnessZ = startSpring[2];
            startRelease.RotationalStiffnessX = startSpring[3];
            startRelease.RotationalStiffnessY = startSpring[4];
            startRelease.RotationalStiffnessZ = startSpring[5];

            Constraint6DOF endRelease = new Constraint6DOF();

            endRelease.TranslationX = endRestraint[0] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.TranslationY = endRestraint[1] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.TranslationZ = endRestraint[2] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.RotationX = endRestraint[3] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.RotationY = endRestraint[4] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.RotationZ = endRestraint[5] == true ? DOFType.Free : DOFType.Fixed;

            endRelease.TranslationalStiffnessX = endSpring[0];
            endRelease.TranslationalStiffnessY = endSpring[1];
            endRelease.TranslationalStiffnessZ = endSpring[2];
            endRelease.RotationalStiffnessX = endSpring[3];
            endRelease.RotationalStiffnessY = endSpring[4];
            endRelease.RotationalStiffnessZ = endSpring[5];

            BarRelease barRelease = new BarRelease() { StartRelease = startRelease, EndRelease = endRelease };

            return barRelease;
        }
    }
}
