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

using BH.Engine.Adapter;
using BH.Engine.Base;
using BH.Engine.Structure;
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Fragments;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Springs;
using BH.oM.Structure.Springs.NonLinearBehaviour;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;


namespace BH.Adapter.ETABS
{
#if Debug16 || Release16
    public partial class ETABS2016Adapter : BHoMAdapter
#elif Debug17 || Release17
   public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /***    Create Methods                           ***/
        /***************************************************/

        private bool CreateObject(ISpringProperty springProperty)
        {
            // Dispatch on the concrete spring-property type. Point springs are supported today;
            // line and area springs can be added later as their own CreateSpringProperty overloads.
            return CreateSpringProperty(springProperty as dynamic);
        }

        /***************************************************/

        private bool CreateSpringProperty(PointSpringProperty spring)
        {
            return CreatePointSpringProperty(spring);
        }

        /***************************************************/

        // Fallback for spring-property types that don't yet have a dedicated creator (e.g. line, area).
        private bool CreateSpringProperty(ISpringProperty spring)
        {
            Engine.Base.Compute.RecordWarning($"Spring properties of type {spring.GetType().Name} are not yet supported by the ETABS adapter.");
            return false;
        }

        /***************************************************/
        /***    Layer 2 - Point spring property          ***/
        /***************************************************/

        // Creates the named ETABS point spring property (PropPointSpring) and wires in the per-axis
        // link properties (Layer 1). Does NOT assign the property to any element - that is Layer 3,
        // handled by the calling element (e.g. Node.SetObject via SetSpringAssignment).
        private bool CreatePointSpringProperty(PointSpringProperty spring)
        {
            string propName = spring.DescriptionOrName();

            // Never create an unnamed ETABS property: an empty name can't be reused as a key, so ETABS
            // would spawn a fresh unnamed property on every call (one per node). Fail loudly instead.
            if (string.IsNullOrWhiteSpace(propName))
            {
                Engine.Base.Compute.RecordWarning("A PointSpringProperty has no Name and no usable description, so a named ETABS point spring property cannot be created. Set a Name on the PointSpringProperty. The spring was not created.");
                return false;
            }

            // Store the ETABS name on the object so element assignments (e.g. Node) reference the exact
            // same name via GetAdapterId, rather than re-deriving it independently.
            SetAdapterId(spring, propName);

            // Linear point spring: all effective stiffness lives on the property. ETABS present units are set
            // to N, m (see ETABSAdapter), so the SI stiffnesses are passed through unconverted.
            if (spring.NonlinearBehaviour == null)
            {
                double[] kLinear =
                {
                    spring.TranslationalStiffnessX,
                    spring.TranslationalStiffnessY,
                    spring.TranslationalStiffnessZ,
                    spring.RotationalStiffnessX,
                    spring.RotationalStiffnessY,
                    spring.RotationalStiffnessZ,
                };

                if (m_model.PropPointSpring.SetPointSpringProp(propName, 1, ref kLinear) != 0)
                    CreatePropertyWarning("Point Spring", "PointSpringProperty", propName);

                return true;
            }

            // Nonlinear behaviour is realised as one single-joint link per axis (X, Y, Z), each carrying the
            // translational (U1) and rotational (R1) behaviour for that axis. LinkAxialDir 1=+X, 2=+Y, 3=+Z.
            // Track which of the 6 DOFs a link ends up carrying, so the property stiffness is not duplicated
            // on those DOFs (the link holds their Ke); DOFs no link carries keep their Ke on the property.
            List<string> linkNames = new List<string>();
            List<int> axialDirs = new List<int>();
            List<double> linkAngles = new List<double>();
            bool[] linkCoversDof = new bool[6];

            for (int axis = 0; axis < 3; axis++)
            {
                string linkName = propName + "_" + "XYZ"[axis];

                // Layer 1: create the link property for this axis, dispatched by behaviour type.
                (bool coversTranslation, bool coversRotation) = CreateBehaviourLink(linkName, spring, axis);
                if (!coversTranslation && !coversRotation)
                    continue;

                linkNames.Add(linkName);
                axialDirs.Add(axis + 1);
                linkAngles.Add(0.0);

                if (coversTranslation) linkCoversDof[axis] = true;      // U1..U3 -> indices 0,1,2
                if (coversRotation) linkCoversDof[axis + 3] = true;     // R1..R3 -> indices 3,4,5
            }

            // Property carries Ke only for DOFs no link took over (keeps linear-only DOFs); DOFs carried by a
            // link are zeroed here to avoid duplicating their stiffness.
            double[] k =
            {
                linkCoversDof[0] ? 0.0 : spring.TranslationalStiffnessX,
                linkCoversDof[1] ? 0.0 : spring.TranslationalStiffnessY,
                linkCoversDof[2] ? 0.0 : spring.TranslationalStiffnessZ,
                linkCoversDof[3] ? 0.0 : spring.RotationalStiffnessX,
                linkCoversDof[4] ? 0.0 : spring.RotationalStiffnessY,
                linkCoversDof[5] ? 0.0 : spring.RotationalStiffnessZ,
            };

            if (m_model.PropPointSpring.SetPointSpringProp(propName, 1, ref k) != 0)
                CreatePropertyWarning("Point Spring", "PointSpringProperty", propName);

            // Wire the created links into the point spring property (the property must exist first).
            if (linkNames.Count > 0)
            {
                string[] linkNamesArr = linkNames.ToArray();
                int[] axialDirsArr = axialDirs.ToArray();
                double[] anglesArr = linkAngles.ToArray();

                if (m_model.PropPointSpring.SetLinks(propName, linkNames.Count, ref linkNamesArr, ref axialDirsArr, ref anglesArr) != 0)
                    CreatePropertyWarning("NonLinear Spring Links", "PointSpringProperty", propName);
            }

            return true;
        }

        /***************************************************/
        /***    Behaviour dispatch                       ***/
        /***************************************************/

        // Creates the link property for a single axis (0=X, 1=Y, 2=Z), dispatched to the matching PropLink
        // type. Returns which DOFs (U1 translation, R1 rotation) the created link carries, so the caller can
        // avoid duplicating their stiffness on the point spring property. (false, false) => nothing created.
        private (bool Translation, bool Rotation) CreateBehaviourLink(string linkName, PointSpringProperty spring, int axis)
        {
            INonLinearBehaviour behaviour = spring.NonlinearBehaviour;

            if (behaviour is MultiLinearElasticBehaviour || behaviour is MultiLinearPlasticBehaviour)
                return CreateMultiLinearSpringProperty(linkName, spring, axis);

            if (behaviour is GapBehaviour || behaviour is HookBehaviour)
                return CreateGapOrHookLinkProperty(linkName, spring, axis);

            if (behaviour is DamperBehaviour)
                return CreateDamperLinkProperty(linkName, spring, axis);

            // No PropLink mapping for this behaviour type. Warn once (first axis only).
            if (axis == 0)
                Engine.Base.Compute.RecordWarning($"Nonlinear spring behaviour '{behaviour.GetType().Name}' is not yet supported by the ETABS adapter and was skipped.");

            return (false, false);
        }

        /***************************************************/

        // Per-axis accessors for the two per-DOF representations. axis: 0=X, 1=Y, 2=Z.
        private static (List<ForceDeformationPoint> Translation, List<ForceDeformationPoint> Rotation) CurvesForAxis(ForceDeformationCurves c, int axis)
        {
            switch (axis)
            {
                case 0: return (c.TranslationX, c.RotationX);
                case 1: return (c.TranslationY, c.RotationY);
                default: return (c.TranslationZ, c.RotationZ);
            }
        }

        private static (double Translation, double Rotation) ValuesForAxis(NonlinearSpringValues v, int axis)
        {
            switch (axis)
            {
                case 0: return (v.TranslationX, v.RotationX);
                case 1: return (v.TranslationY, v.RotationY);
                default: return (v.TranslationZ, v.RotationZ);
            }
        }

        private static (double Translation, double Rotation) ValuesForAxis(PointSpringProperty p, int axis)
        {
            switch (axis)
            {
                case 0: return (p.TranslationalStiffnessX, p.RotationalStiffnessX);
                case 1: return (p.TranslationalStiffnessY, p.RotationalStiffnessY);
                default: return (p.TranslationalStiffnessZ, p.RotationalStiffnessZ);
            }
        }

        /***************************************************/

        // Builds the ETABS link DOF / fixity / nonlinearity flag arrays for one axis (U1 = translation,
        // R1 = rotation), from the Constraint6DOF restraints and whether the behaviour defines nonlinear
        // data on each DOF. A fixed DOF is activated and marked fixed (its nonlinear data discarded, with a
        // warning); a free DOF with nonlinear data is activated as nonlinear; a free DOF with no data stays
        // inactive. Nonlinearity cannot coexist with a fixed DOF in ETABS.
        private (bool[] Dof, bool[] Fix, bool[] NonLin) LinkDofFlags(PointSpringProperty spring, int axis, bool translationHasData, bool rotationHasData, string linkName)
        {
            bool fixedT, fixedR;
            switch (axis)
            {
                case 0: fixedT = spring.TranslationX == DOFType.Fixed; fixedR = spring.RotationX == DOFType.Fixed; break;
                case 1: fixedT = spring.TranslationY == DOFType.Fixed; fixedR = spring.RotationY == DOFType.Fixed; break;
                default: fixedT = spring.TranslationZ == DOFType.Fixed; fixedR = spring.RotationZ == DOFType.Fixed; break;
            }

            char axisName = "XYZ"[axis];

            if (translationHasData && fixedT)
            {
                Engine.Base.Compute.RecordWarning($"The translational DOF along {axisName} is fixed on '{linkName}', so its nonlinear spring behaviour was discarded - nonlinearity cannot exist on a fixed degree of freedom.");
                translationHasData = false;
            }

            if (rotationHasData && fixedR)
            {
                Engine.Base.Compute.RecordWarning($"The rotational DOF about {axisName} is fixed on '{linkName}', so its nonlinear spring behaviour was discarded - nonlinearity cannot exist on a fixed degree of freedom.");
                rotationHasData = false;
            }

            // A DOF is active if it is fixed (rigid) or carries nonlinear behaviour.
            bool[] dof = { fixedT || translationHasData, false, false, fixedR || rotationHasData, false, false };
            bool[] fix = { fixedT, false, false, fixedR, false, false };
            bool[] nonLin = { translationHasData, false, false, rotationHasData, false, false };

            return (dof, fix, nonLin);
        }

        /***************************************************/
        /***    Layer 1 - Link property                  ***/
        /***************************************************/

        // Creates a single ETABS multilinear (elastic or plastic) link property for one axis, in the same
        // shape as the gap creator: effective terms (Ke, Ce) come from the PointSpringProperty, and the
        // nonlinear response comes from the behaviour's force-deformation curves. U1 (index 0) carries
        // translation, R1 (index 3) carries rotation about the same axis.
        // Returns which DOFs (U1 translation, R1 rotation) the link carries; (false, false) if none.
        private (bool Translation, bool Rotation) CreateMultiLinearSpringProperty(string linkName, PointSpringProperty spring, int axis)
        {
            bool isPlastic;
            ForceDeformationCurves curves;
            switch (spring.NonlinearBehaviour)
            {
                case MultiLinearElasticBehaviour e: curves = e.ForceDeformationCurves; isPlastic = false; break;
                case MultiLinearPlasticBehaviour p: curves = p.ForceDeformationCurves; isPlastic = true; break;
                default: return (false, false);
            }

            (List<ForceDeformationPoint> translation, List<ForceDeformationPoint> rotation) = CurvesForAxis(curves, axis);

            // DOF activation, fixity (from the Constraint6DOF restraints) and nonlinearity flags for this axis.
            (bool[] dof, bool[] fix, bool[] nonLin) = LinkDofFlags(spring, axis, translation?.Count >= 2, rotation?.Count >= 2, linkName);

            // Only create a link when the axis actually has nonlinear behaviour. A fixed DOF alone must not
            // spawn a rigid link; its fixity belongs on the node restraint, not a spring link.
            if (!nonLin[0] && !nonLin[3])
                return (false, false);

            // Effective (linear-analysis) terms from the point spring property.
            (double effStiffT, double effStiffR) = ValuesForAxis(spring, axis);                 // Ke
            (double effDampT, double effDampR) = ValuesForAxis(spring.EffectiveDamping, axis);   // Ce

            // ETABS present units are N, m, so SI terms are passed through unconverted.
            double[] ke = { effStiffT, 0, 0, effStiffR, 0, 0 };
            double[] ce = { effDampT, 0, 0, effDampR, 0, 0 };

            int ret = isPlastic
                ? m_model.PropLink.SetMultiLinearPlastic(linkName, ref dof, ref fix, ref nonLin, ref ke, ref ce, 0, 0)
                : m_model.PropLink.SetMultiLinearElastic(linkName, ref dof, ref fix, ref nonLin, ref ke, ref ce, 0, 0);

            if (ret != 0)
            {
                CreatePropertyWarning("MultiLinear Link", "PointSpringProperty", linkName);
                return (false, false);
            }

            // MyType for SetMultiLinearPoints must be 1, 2 or 3 (1 = Kinematic). It only affects the plastic
            // case; for elastic any valid value works, so pass 1 (passing 0 would fail the call).
            HysteresisType hysteresisType = spring.FindFragment<PointSpringNonlinearity>()?.SpringHysteresisType ?? HysteresisType.Kinematic;
            int hysteresisInt = isPlastic ? (int)hysteresisType : 1;

            // Translational curve on U1. Forces are SI (N) and passed through; deformations are in metres.
            if (nonLin[0])
            {
                double[] F = translation.Select(p => p.Force).ToArray();
                double[] D = translation.Select(p => p.Deformation).ToArray();

                if (m_model.PropLink.SetMultiLinearPoints(linkName, 1, translation.Count, ref F, ref D, hysteresisInt) != 0)
                    CreatePropertyWarning("MultiLinear Link Translation Points", "PointSpringProperty", linkName);
            }

            // Rotational curve on R1. Forces are SI (N·m) and passed through; deformations are in radians.
            if (nonLin[3])
            {
                double[] F = rotation.Select(p => p.Force).ToArray();
                double[] D = rotation.Select(p => p.Deformation).ToArray();

                if (m_model.PropLink.SetMultiLinearPoints(linkName, 4, rotation.Count, ref F, ref D, hysteresisInt) != 0)
                    CreatePropertyWarning("MultiLinear Link Rotation Points", "PointSpringProperty", linkName);
            }

            return (dof[0], dof[3]);
        }

        /***************************************************/

        // Creates a single ETABS gap (compression-only) or hook (tension-only) link property for one axis.
        // The two behaviours share an identical PropLink shape (Ke, Ce, K, Dis), differing only in the CSI
        // call used, so they are handled together. U1 (index 0) carries translation, R1 (index 3) carries
        // rotation about the same axis. Effective terms (Ke, Ce) come from the PointSpringProperty
        // (Constraint6DOF stiffness and EffectiveDamping); the nonlinear stiffness K and initial opening Dis
        // come from the behaviour. Returns false if the axis has no gap/hook defined (or creation failed).
        private (bool Translation, bool Rotation) CreateGapOrHookLinkProperty(string linkName, PointSpringProperty spring, int axis)
        {
            NonlinearSpringValues initialStiffness;
            NonlinearSpringValues initialOpening;
            bool isHook;
            switch (spring.NonlinearBehaviour)
            {
                case GapBehaviour g: initialStiffness = g.InitialStiffness; initialOpening = g.InitialOpening; isHook = false; break;
                case HookBehaviour h: initialStiffness = h.InitialStiffness; initialOpening = h.InitialOpening; isHook = true; break;
                default: return (false, false);
            }

            // Nonlinear gap/hook parameters from the behaviour.
            (double stiffnessT, double stiffnessR) = ValuesForAxis(initialStiffness, axis);       // K
            (double openingT, double openingR) = ValuesForAxis(initialOpening, axis);             // Dis

            // Effective (linear-analysis) terms from the point spring property.
            (double effStiffT, double effStiffR) = ValuesForAxis(spring, axis);                   // Ke (Constraint6DOF)
            (double effDampT, double effDampR) = ValuesForAxis(spring.EffectiveDamping, axis);    // Ce

            // DOF activation, fixity (from the Constraint6DOF restraints) and nonlinearity flags for this axis.
            (bool[] dof, bool[] fix, bool[] nonLin) = LinkDofFlags(spring, axis, stiffnessT != 0 || openingT != 0, stiffnessR != 0 || openingR != 0, linkName);

            // Only create a link when the axis actually has nonlinear behaviour. A fixed DOF alone must not
            // spawn a rigid link; its fixity belongs on the node restraint, not a spring link.
            if (!nonLin[0] && !nonLin[3])
                return (false, false);

            // ETABS present units are N, m, so SI terms are passed through unconverted; the initial opening is
            // a deformation (m / rad) and is likewise unchanged.
            double[] ke = { effStiffT, 0, 0, effStiffR, 0, 0 };
            double[] ce = { effDampT, 0, 0, effDampR, 0, 0 };
            double[] k = { stiffnessT, 0, 0, stiffnessR, 0, 0 };
            double[] dis = { openingT, 0, 0, openingR, 0, 0 };

            int ret = isHook
                ? m_model.PropLink.SetHook(linkName, ref dof, ref fix, ref nonLin, ref ke, ref ce, ref k, ref dis, 0, 0)
                : m_model.PropLink.SetGap(linkName, ref dof, ref fix, ref nonLin, ref ke, ref ce, ref k, ref dis, 0, 0);

            if (ret != 0)
            {
                CreatePropertyWarning(isHook ? "Hook Link" : "Gap Link", "PointSpringProperty", linkName);
                return (false, false);
            }

            return (dof[0], dof[3]);
        }

        /***************************************************/

        // Creates a single ETABS damper (viscous) link property for one axis. U1 (index 0) carries
        // translation, R1 (index 3) carries rotation about the same axis. Effective terms (Ke, Ce) come from
        // the PointSpringProperty (Constraint6DOF stiffness and EffectiveDamping); the nonlinear spring
        // stiffness K, damping coefficient C and damping exponent CExp come from the behaviour.
        // Returns false if the axis has no damper defined (or creation failed).
        private (bool Translation, bool Rotation) CreateDamperLinkProperty(string linkName, PointSpringProperty spring, int axis)
        {
            if (!(spring.NonlinearBehaviour is DamperBehaviour damper))
                return (false, false);

            // Nonlinear damper parameters from the behaviour.
            (double stiffnessT, double stiffnessR) = ValuesForAxis(damper.InitialStiffness, axis);    // K
            (double dampCoeffT, double dampCoeffR) = ValuesForAxis(damper.DampingCoefficient, axis);  // C
            (double dampExpT, double dampExpR) = ValuesForAxis(damper.DampingExponent, axis);         // CExp

            // Effective (linear-analysis) terms from the point spring property.
            (double effStiffT, double effStiffR) = ValuesForAxis(spring, axis);                   // Ke (Constraint6DOF)
            (double effDampT, double effDampR) = ValuesForAxis(spring.EffectiveDamping, axis);    // Ce

            // DOF activation, fixity (from the Constraint6DOF restraints) and nonlinearity flags for this axis.
            // A damper DOF is nonlinear when it defines a damping coefficient or a parallel spring stiffness.
            (bool[] dof, bool[] fix, bool[] nonLin) = LinkDofFlags(spring, axis, dampCoeffT != 0 || stiffnessT != 0, dampCoeffR != 0 || stiffnessR != 0, linkName);

            // Only create a link when the axis actually has nonlinear behaviour. A fixed DOF alone must not
            // spawn a rigid link; its fixity belongs on the node restraint, not a spring link.
            if (!nonLin[0] && !nonLin[3])
                return (false, false);

            // ETABS present units are N, m, so SI terms are passed through unconverted; the damping exponent is
            // unitless and is likewise unchanged.
            double[] ke = { effStiffT, 0, 0, effStiffR, 0, 0 };
            double[] ce = { effDampT, 0, 0, effDampR, 0, 0 };
            double[] k = { stiffnessT, 0, 0, stiffnessR, 0, 0 };
            double[] c = { dampCoeffT, 0, 0, dampCoeffR, 0, 0 };
            double[] cexp = { dampExpT, 0, 0, dampExpR, 0, 0 };

            if (m_model.PropLink.SetDamper(linkName, ref dof, ref fix, ref nonLin, ref ke, ref ce, ref k, ref c, ref cexp, 0, 0) != 0)
            {
                CreatePropertyWarning("Damper Link", "PointSpringProperty", linkName);
                return (false, false);
            }

            return (dof[0], dof[3]);
        }

        /***************************************************/
    }
}
