/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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
using System.ComponentModel;
using BH.oM.Analytical.Results;
using BH.oM.Quantities.Attributes;
using BH.oM.Structure.Results;

namespace BH.oM.Adapters.ETABS.Results
{
    [Description("Resultant forces and moments acting on an ETABS section cut for a given Loadcase or LoadCombination.")]
    public class SectionCutForce : IStructuralResult, IResultItem
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        [ObjectIdentifier]
        [Description("Name of the section cut as defined in the ETABS model.")]
        public virtual IComparable ObjectId { get; }

        [ScenarioIdentifier]
        [Description("Identifier for the Loadcase or LoadCombination that the result belongs to. Is generally name or number of the loadcase, depending on the analysis package.")]
        public virtual IComparable ResultCase { get; }

        [ScenarioIdentifier]
        [Description("Positive index, starting at one. Only set for cases with modal outputs such as dynamic cases.")]
        public virtual int ModeNumber { get; }

        [ScenarioIdentifier]
        [Description("Time step for time history results.")]
        public virtual double TimeStep { get; }

        [Description("Step type reported by ETABS for this result, for example Max, Min or the step of a time history case.")]
        public virtual string StepType { get; } = "";

        [Force]
        [Description("Resultant force along the first axis. Global X for analysis results, axial force P for design results.")]
        public virtual double FX { get; }

        [Force]
        [Description("Resultant force along the second axis. Global Y for analysis results, shear V2 for design results.")]
        public virtual double FY { get; }

        [Force]
        [Description("Resultant force along the third axis. Global Z for analysis results, shear V3 for design results.")]
        public virtual double FZ { get; }

        [Moment]
        [Description("Resultant moment about the first axis. Global X for analysis results, torsion T for design results.")]
        public virtual double MX { get; }

        [Moment]
        [Description("Resultant moment about the second axis. Global Y for analysis results, bending M2 for design results.")]
        public virtual double MY { get; }

        [Moment]
        [Description("Resultant moment about the third axis. Global Z for analysis results, bending M3 for design results.")]
        public virtual double MZ { get; }

        /***************************************************/
        /**** Constructors                              ****/
        /***************************************************/

        public SectionCutForce(IComparable objectId, IComparable resultCase, int modeNumber, double timeStep, string stepType, double fx, double fy, double fz, double mx, double my, double mz)
        {
            ObjectId = objectId;
            ResultCase = resultCase;
            ModeNumber = modeNumber;
            TimeStep = timeStep;
            StepType = stepType;
            FX = fx;
            FY = fy;
            FZ = fz;
            MX = mx;
            MY = my;
            MZ = mz;
        }

        /***************************************************/
        /**** IComparable Interface                     ****/
        /***************************************************/

        [Description("Controls how this result is sorted in relation to other results. Sorts with the following priority: Type, ObjectId, ResultCase, ModeNumber, TimeStep.")]
        public int CompareTo(IResult other)
        {
            // Fall back to sorting by type name when the other result is not a section cut
            SectionCutForce otherRes = other as SectionCutForce;

            if (otherRes == null)
                return this.GetType().Name.CompareTo(other.GetType().Name);

            int n = this.ObjectId.CompareTo(otherRes.ObjectId);
            if (n != 0)
                return n;

            int l = this.ResultCase.CompareTo(otherRes.ResultCase);
            if (l != 0)
                return l;

            int m = this.ModeNumber.CompareTo(otherRes.ModeNumber);
            return m == 0 ? this.TimeStep.CompareTo(otherRes.TimeStep) : m;
        }

        /***************************************************/
    }
}
