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
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Fragments;
using BH.oM.Structure.Springs;
using BH.oM.Structure.Springs.NonLinearBehaviour;
using CSiAPIv1;
using System;
using System.Collections.Generic;


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
        /***    Read Methods                             ***/
        /***************************************************/

        private List<PointSpringProperty> ReadSpringProperty(List<string> ids = null)
        {
            List<PointSpringProperty> springList = new List<PointSpringProperty>();

            int nameCount = 0;
            string[] nameArr = { };
            m_model.PropPointSpring.GetNameList(ref nameCount, ref nameArr);

            ids = FilterIds(ids, nameArr);

            foreach (string id in ids)
            {
                PointSpringProperty spring = ReadPointSpringProperty(id);
                if (spring != null)
                    springList.Add(spring);
            }

            return springList;
        }

        /***************************************************/

        // Reads a single named point spring property by name. Shared by ReadSpringProperty (standalone)
        // and ReadNode (embedded via SetSpringAssignment). Returns null if the property does not exist
        // or is not a user-specified property (SpringOption 1). Handles both purely linear point springs
        // (stiffness only) and link-based nonlinear springs (force-deformation curves + ETABS fragment).
        private PointSpringProperty ReadPointSpringProperty(string springPropName)
        {
            // Get point spring property — confirm SpringOption = 1 (user specified / link based).
            int springOpt = 0;
            double[] k = null;
            string cSys = "", soilProfile = "", footing = "", notes = "", guid = "";
            double period = 0;
            int color = 0;

            if (m_model.PropPointSpring.GetPointSpringProp(
                    springPropName, ref springOpt, ref k,
                    ref cSys, ref soilProfile, ref footing,
                    ref period, ref color, ref notes, ref guid) != 0
                || springOpt != 1 || k == null)
                return null;

            PointSpringProperty spring = new PointSpringProperty();
            spring.Name = springPropName;

            // Populate stiffness from k[]. ETABS present units are N, m, so the values are already SI.
            spring.TranslationalStiffnessX = k[0];
            spring.TranslationalStiffnessY = k[1];
            spring.TranslationalStiffnessZ = k[2];
            spring.RotationalStiffnessX = k[3];
            spring.RotationalStiffnessY = k[4];
            spring.RotationalStiffnessZ = k[5];

            // Read attached link properties. A purely linear point spring has none — in that case the
            // spring is returned with stiffness only.
            int nLinks = 0;
            string[] linkNames = null;
            int[] axialDirs = null;
            double[] angles = null;

            if (m_model.PropPointSpring.GetLinks(springPropName, ref nLinks,
                    ref linkNames, ref axialDirs, ref angles) == 0
                && nLinks > 0 && linkNames != null)
            {
                spring.NonlinearBehaviour = ReadNonLinearBehaviour(spring, linkNames, axialDirs);
            }

            SetAdapterId(spring, springPropName);
            return spring;
        }

        /***************************************************/
        /***    Behaviour dispatch                       ***/
        /***************************************************/

        // Determines the shared link type of the axis links attached to a point spring and reads the matching
        // nonlinear behaviour. All axis links created for a single BHoM spring share one link type, so the type
        // of the first readable link decides the behaviour. Returns null if no link type could be read or the
        // type is not one the adapter maps to a behaviour.
        private INonLinearBehaviour ReadNonLinearBehaviour(PointSpringProperty spring, string[] linkNames, int[] axialDirs)
        {
            eLinkPropType type = eLinkPropType.Linear;
            bool found = false;
            for (int i = 0; i < linkNames.Length; i++)
            {
                eLinkPropType t = eLinkPropType.Linear;
                if (m_model.PropLink.GetTypeOAPI(linkNames[i], ref t) == 0)
                {
                    type = t;
                    found = true;
                    break;
                }
            }

            if (!found)
                return null;

            switch (type)
            {
                case eLinkPropType.MultilinearElastic:
                case eLinkPropType.MultilinearPlastic:
                    return ReadMultiLinearBehaviour(spring, linkNames, axialDirs, type == eLinkPropType.MultilinearPlastic);
                case eLinkPropType.Gap:
                case eLinkPropType.Hook:
                    return ReadGapOrHookBehaviour(spring, linkNames, axialDirs, type == eLinkPropType.Hook);
                case eLinkPropType.Damper:
                    return ReadDamperBehaviour(spring, linkNames, axialDirs);
                default:
                    return null;
            }
        }

        /***************************************************/
        /***    Per-type link readers                    ***/
        /***************************************************/

        // Reads the multilinear (elastic or plastic) force-deformation curves from the axis links, collecting
        // them per axis into one container. The effective terms (Ke, Ce) held on each link are read back onto
        // the spring, and the ETABS-specific hysteresis (meaningful only for plastic) is attached as a fragment.
        private INonLinearBehaviour ReadMultiLinearBehaviour(PointSpringProperty spring, string[] linkNames, int[] axialDirs, bool isPlastic)
        {
            ForceDeformationCurves curves = new ForceDeformationCurves();
            int hysteresisInt = 0;

            for (int i = 0; i < linkNames.Length; i++)
            {
                string linkName = linkNames[i];
                int axialDir = Math.Abs(axialDirs[i]);

                // Effective stiffness/damping (Ke, Ce) live on the link for nonlinear DOFs; read them back
                // so the spring stiffness and EffectiveDamping round-trip.
                bool[] dof = null, fix = null, nonlin = null;
                double[] ke = null, ce = null;
                double dj2 = 0, dj3 = 0;
                string notes = "", guid = "";

                int ret = isPlastic
                    ? m_model.PropLink.GetMultiLinearPlastic(linkName, ref dof, ref fix, ref nonlin, ref ke, ref ce, ref dj2, ref dj3, ref notes, ref guid)
                    : m_model.PropLink.GetMultiLinearElastic(linkName, ref dof, ref fix, ref nonlin, ref ke, ref ce, ref dj2, ref dj3, ref notes, ref guid);
                if (ret == 0)
                    PopulateEffectiveTerms(spring, axialDir, dof, fix, ke, ce);

                // Translational curve on U1 (dof 1) and rotational curve on R1 (dof 4).
                List<ForceDeformationPoint> tCurve = ReadMultiLinearCurve(linkName, 1, ref hysteresisInt);
                if (tCurve != null)
                    SetCurveForAxis(curves, axialDir, false, tCurve);

                List<ForceDeformationPoint> rCurve = ReadMultiLinearCurve(linkName, 4, ref hysteresisInt);
                if (rCurve != null)
                    SetCurveForAxis(curves, axialDir, true, rCurve);
            }

            // ETABS-specific hysteresis (only meaningful for plastic) as a fragment.
            spring.Fragments.Add(new PointSpringNonlinearity
            {
                SpringHysteresisType = hysteresisInt > 0 ? (HysteresisType)hysteresisInt : HysteresisType.Kinematic
            });

            return isPlastic
                ? (INonLinearBehaviour)new MultiLinearPlasticBehaviour { ForceDeformationCurves = curves }
                : new MultiLinearElasticBehaviour { ForceDeformationCurves = curves };
        }

        /***************************************************/

        // Reads a gap (compression-only) or hook (tension-only) behaviour from the axis links. The two share an
        // identical PropLink shape (Ke, Ce, K, Dis), so they are read together and differ only in the CSI call
        // and the behaviour type produced. Nonlinear stiffness K and initial opening Dis are collected per axis;
        // the effective terms (Ke, Ce) are read back onto the spring.
        private INonLinearBehaviour ReadGapOrHookBehaviour(PointSpringProperty spring, string[] linkNames, int[] axialDirs, bool isHook)
        {
            NonlinearSpringValues initialStiffness = new NonlinearSpringValues();
            NonlinearSpringValues initialOpening = new NonlinearSpringValues();

            for (int i = 0; i < linkNames.Length; i++)
            {
                string linkName = linkNames[i];
                int axialDir = Math.Abs(axialDirs[i]);

                bool[] dof = null, fix = null, nonlin = null;
                double[] ke = null, ce = null, k = null, dis = null;
                double dj2 = 0, dj3 = 0;
                string notes = "", guid = "";

                int ret = isHook
                    ? m_model.PropLink.GetHook(linkName, ref dof, ref fix, ref nonlin, ref ke, ref ce, ref k, ref dis, ref dj2, ref dj3, ref notes, ref guid)
                    : m_model.PropLink.GetGap(linkName, ref dof, ref fix, ref nonlin, ref ke, ref ce, ref k, ref dis, ref dj2, ref dj3, ref notes, ref guid);
                if (ret != 0)
                    continue;

                PopulateEffectiveTerms(spring, axialDir, dof, fix, ke, ce);

                // Translation on U1 (index 0), rotation on R1 (index 3). ETABS present units are N, m, so K and
                // the opening Dis are already SI.
                if (HasNonlinear(nonlin, 0))
                {
                    SetAxisValue(initialStiffness, axialDir, false, k[0]);
                    SetAxisValue(initialOpening, axialDir, false, dis[0]);
                }
                if (HasNonlinear(nonlin, 3))
                {
                    SetAxisValue(initialStiffness, axialDir, true, k[3]);
                    SetAxisValue(initialOpening, axialDir, true, dis[3]);
                }
            }

            return isHook
                ? (INonLinearBehaviour)new HookBehaviour { InitialStiffness = initialStiffness, InitialOpening = initialOpening }
                : new GapBehaviour { InitialStiffness = initialStiffness, InitialOpening = initialOpening };
        }

        /***************************************************/

        // Reads a damper (viscous) behaviour from the axis links. The parallel spring stiffness K, damping
        // coefficient C and damping exponent CExp are collected per axis; the effective terms (Ke, Ce) are read
        // back onto the spring.
        private INonLinearBehaviour ReadDamperBehaviour(PointSpringProperty spring, string[] linkNames, int[] axialDirs)
        {
            NonlinearSpringValues initialStiffness = new NonlinearSpringValues();
            NonlinearSpringValues dampingCoefficient = new NonlinearSpringValues();
            NonlinearSpringValues dampingExponent = new NonlinearSpringValues();

            for (int i = 0; i < linkNames.Length; i++)
            {
                string linkName = linkNames[i];
                int axialDir = Math.Abs(axialDirs[i]);

                bool[] dof = null, fix = null, nonlin = null;
                double[] ke = null, ce = null, k = null, c = null, cexp = null;
                double dj2 = 0, dj3 = 0;
                string notes = "", guid = "";

                if (m_model.PropLink.GetDamper(linkName, ref dof, ref fix, ref nonlin, ref ke, ref ce, ref k, ref c, ref cexp, ref dj2, ref dj3, ref notes, ref guid) != 0)
                    continue;

                PopulateEffectiveTerms(spring, axialDir, dof, fix, ke, ce);

                // Translation on U1 (index 0), rotation on R1 (index 3). ETABS present units are N, m, so K and C
                // are already SI; the damping exponent is unitless.
                if (HasNonlinear(nonlin, 0))
                {
                    SetAxisValue(initialStiffness, axialDir, false, k[0]);
                    SetAxisValue(dampingCoefficient, axialDir, false, c[0]);
                    SetAxisValue(dampingExponent, axialDir, false, cexp[0]);
                }
                if (HasNonlinear(nonlin, 3))
                {
                    SetAxisValue(initialStiffness, axialDir, true, k[3]);
                    SetAxisValue(dampingCoefficient, axialDir, true, c[3]);
                    SetAxisValue(dampingExponent, axialDir, true, cexp[3]);
                }
            }

            return new DamperBehaviour
            {
                InitialStiffness = initialStiffness,
                DampingCoefficient = dampingCoefficient,
                DampingExponent = dampingExponent
            };
        }

        /***************************************************/
        /***    Read helpers                             ***/
        /***************************************************/

        // Reads a single multilinear force-deformation curve from the given link DOF (1 = U1, 4 = R1). ETABS
        // present units are N, m, so the values are already SI. Returns null if the DOF has no curve.
        private List<ForceDeformationPoint> ReadMultiLinearCurve(string linkName, int dof, ref int hysteresisInt)
        {
            int nPts = 0;
            double[] F = null, D = null;
            double a1 = 0, a2 = 0, b1 = 0, b2 = 0, eta = 0;

            if (m_model.PropLink.GetMultiLinearPoints(linkName, dof, ref nPts, ref F, ref D,
                    ref hysteresisInt, ref a1, ref a2, ref b1, ref b2, ref eta) != 0
                || F == null || D == null)
                return null;

            List<ForceDeformationPoint> curve = new List<ForceDeformationPoint>();
            for (int j = 0; j < Math.Min(D.Length, F.Length); j++)
                curve.Add(new ForceDeformationPoint { Deformation = D[j], Force = F[j] });

            return curve;
        }

        /***************************************************/

        // Reads the effective (linear-analysis) stiffness Ke and damping Ce a link carries back onto the spring,
        // for the DOFs the link owns (active and not fixed). Translation is on U1 (index 0), rotation on R1
        // (index 3). ETABS present units are N, m, so the values are already SI.
        private static void PopulateEffectiveTerms(PointSpringProperty spring, int axialDir, bool[] dof, bool[] fix, double[] ke, double[] ce)
        {
            if (dof == null)
                return;

            bool translationOwned = dof.Length > 0 && dof[0] && !(fix != null && fix.Length > 0 && fix[0]);
            bool rotationOwned = dof.Length > 3 && dof[3] && !(fix != null && fix.Length > 3 && fix[3]);

            if (translationOwned)
            {
                if (ke != null && ke.Length > 0) SetStiffnessForAxis(spring, axialDir, false, ke[0]);
                if (ce != null && ce.Length > 0) SetAxisValue(spring.EffectiveDamping, axialDir, false, ce[0]);
            }
            if (rotationOwned)
            {
                if (ke != null && ke.Length > 3) SetStiffnessForAxis(spring, axialDir, true, ke[3]);
                if (ce != null && ce.Length > 3) SetAxisValue(spring.EffectiveDamping, axialDir, true, ce[3]);
            }
        }

        /***************************************************/

        // True when the link's Nonlinear flag array marks the given DOF index (0 = U1, 3 = R1) as nonlinear.
        private static bool HasNonlinear(bool[] nonlin, int index)
        {
            return nonlin != null && nonlin.Length > index && nonlin[index];
        }

        /***************************************************/

        // Assigns a force-deformation curve to the axis/DOF slot (1 = X, 2 = Y, 3 = Z) of the curve container.
        private static void SetCurveForAxis(ForceDeformationCurves curves, int axialDir, bool rotation, List<ForceDeformationPoint> curve)
        {
            switch (axialDir)
            {
                case 1: if (rotation) curves.RotationX = curve; else curves.TranslationX = curve; break;
                case 2: if (rotation) curves.RotationY = curve; else curves.TranslationY = curve; break;
                case 3: if (rotation) curves.RotationZ = curve; else curves.TranslationZ = curve; break;
            }
        }

        /***************************************************/

        // Assigns a per-DOF value to the axis/DOF slot (1 = X, 2 = Y, 3 = Z) of a NonlinearSpringValues object.
        private static void SetAxisValue(NonlinearSpringValues values, int axialDir, bool rotation, double value)
        {
            switch (axialDir)
            {
                case 1: if (rotation) values.RotationX = value; else values.TranslationX = value; break;
                case 2: if (rotation) values.RotationY = value; else values.TranslationY = value; break;
                case 3: if (rotation) values.RotationZ = value; else values.TranslationZ = value; break;
            }
        }

        /***************************************************/

        // Assigns an effective stiffness to the axis/DOF slot (1 = X, 2 = Y, 3 = Z) of the point spring property
        // (the inherited Constraint6DOF stiffnesses).
        private static void SetStiffnessForAxis(PointSpringProperty spring, int axialDir, bool rotation, double value)
        {
            switch (axialDir)
            {
                case 1: if (rotation) spring.RotationalStiffnessX = value; else spring.TranslationalStiffnessX = value; break;
                case 2: if (rotation) spring.RotationalStiffnessY = value; else spring.TranslationalStiffnessY = value; break;
                case 3: if (rotation) spring.RotationalStiffnessZ = value; else spring.TranslationalStiffnessZ = value; break;
            }
        }

        /***************************************************/
    }
}
