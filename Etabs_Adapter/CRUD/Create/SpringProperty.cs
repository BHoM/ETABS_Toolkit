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
using BH.oM.Structure.Springs;
using System.Collections.Generic;
using System.Linq;


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

            // Read ETABS-specific settings from fragment.
            PointSpringNonlinearity settings = spring.FindFragment<PointSpringNonlinearity>();
            PointSpringNonlinearType springType = settings?.SpringType ?? PointSpringNonlinearType.MultiLinearElastic;
            HysteresisType hysteresisType = settings?.SpringHysteresisType ?? HysteresisType.Kinematic;

            // Convert stiffness from SI (N/m, N·m/rad) to ETABS units (kN/m, kN·m/rad).
            double[] k = new double[]
            {
                spring.TranslationalStiffnessX / 1000.0,
                spring.TranslationalStiffnessY / 1000.0,
                spring.TranslationalStiffnessZ / 1000.0,
                spring.RotationalStiffnessX    / 1000.0,
                spring.RotationalStiffnessY    / 1000.0,
                spring.RotationalStiffnessZ    / 1000.0,
            };

            // Create the named, link-based point spring property (SpringOption 1).
            if (m_model.PropPointSpring.SetPointSpringProp(propName, 1, ref k) != 0)
                CreatePropertyWarning("NonLinear Point Spring", "PointSpringProperty", propName);

            // Group by axis direction — one link per axis, handling both translation and rotation.
            // LinkAxialDir 1=+X, 2=+Y, 3=+Z. U1 = translation, R1 = rotation about same axis.
            var axisMap = new (List<ForceDeformationPoint> Translation, List<ForceDeformationPoint> Rotation, string Suffix, int AxialDir)[]
            {
                (spring.ForceDeformationCurves.TranslationX, spring.ForceDeformationCurves.RotationX, "_X", 1),
                (spring.ForceDeformationCurves.TranslationY, spring.ForceDeformationCurves.RotationY, "_Y", 2),
                (spring.ForceDeformationCurves.TranslationZ, spring.ForceDeformationCurves.RotationZ, "_Z", 3),
            };

            List<string> linkNames = new List<string>();
            List<int> axialDirs = new List<int>();
            List<double> linkAngles = new List<double>();

            foreach (var (translation, rotation, suffix, axialDir) in axisMap)
            {
                string linkName = propName + suffix;

                // Layer 1: create the link property carrying the force-deformation behaviour for this axis.
                if (!CreateSpringLinkProperty(linkName, translation, rotation, springType, hysteresisType))
                    continue;

                linkNames.Add(linkName);
                axialDirs.Add(axialDir);
                linkAngles.Add(0.0);
            }

            // Wire the created links into the point spring property.
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
        /***    Layer 1 - Link property                  ***/
        /***************************************************/

        // Creates a single ETABS link property (cPropLink) carrying the force-deformation behaviour
        // for one axis. U1 (index 0) carries translation, R1 (index 3) carries rotation.
        // Returns true if a link was created, false if there were no curves to define one (or creation failed).
        // NOTE: This is the shared "Layer 1" seam. Link.cs's LinkConstraint -> SetLinear path is a
        //       different link type and is intentionally left untouched; it can be absorbed here later
        //       once a shared BHoM link-property concept exists.
        private bool CreateSpringLinkProperty(string linkName, List<ForceDeformationPoint> translation, List<ForceDeformationPoint> rotation, PointSpringNonlinearType springType, HysteresisType hysteresisType)
        {
            bool hasTranslation = translation?.Count >= 2;
            bool hasRotation = rotation?.Count >= 2;

            if (!hasTranslation && !hasRotation)
                return false;

            // Activate U1 (index 0) for translation, R1 (index 3) for rotation.
            bool[] dof = { hasTranslation, false, false, hasRotation, false, false };
            bool[] fix = { false, false, false, false, false, false };
            bool[] nonLin = { hasTranslation, false, false, hasRotation, false, false };
            double[] stiff = { 0, 0, 0, 0, 0, 0 };
            double[] damp = { 0, 0, 0, 0, 0, 0 };

            int ret;
            if (springType == PointSpringNonlinearType.MultiLinearElastic)
                ret = m_model.PropLink.SetMultiLinearElastic(linkName, ref dof, ref fix, ref nonLin, ref stiff, ref damp, 0, 0);
            else
                ret = m_model.PropLink.SetMultiLinearPlastic(linkName, ref dof, ref fix, ref nonLin, ref stiff, ref damp, 0, 0);

            if (ret != 0)
            {
                CreatePropertyWarning("NonLinear Link", "PointSpringProperty", linkName);
                return false;
            }

            // MyType for SetMultiLinearPoints must be 1, 2 or 3 (1 = Kinematic). It only affects
            // MultiLinearPlastic, but passing 0 (out of range) causes the call to fail for the
            // MultiLinearElastic case, leaving the link without a force-deformation curve.
            int hysteresisInt = springType == PointSpringNonlinearType.MultiLinearPlastic ? (int)hysteresisType : 1;

            // Set translational curve on U1 (dof index 1).
            if (hasTranslation)
            {
                double[] F = translation.Select(p => p.Force / 1000.0).ToArray();
                double[] D = translation.Select(p => p.Deformation).ToArray();

                if (m_model.PropLink.SetMultiLinearPoints(linkName, 1, translation.Count, ref F, ref D, hysteresisInt) != 0)
                    CreatePropertyWarning("NonLinear Link Translation Points", "PointSpringProperty", linkName);
            }

            // Set rotational curve on R1 (dof index 4).
            if (hasRotation)
            {
                double[] F = rotation.Select(p => p.Force / 1000.0).ToArray();
                double[] D = rotation.Select(p => p.Deformation).ToArray();

                if (m_model.PropLink.SetMultiLinearPoints(linkName, 4, rotation.Count, ref F, ref D, hysteresisInt) != 0)
                    CreatePropertyWarning("NonLinear Link Rotation Points", "PointSpringProperty", linkName);
            }

            return true;
        }

        /***************************************************/
    }
}
