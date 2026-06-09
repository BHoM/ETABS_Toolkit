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

            // Populate stiffness from k[] — convert ETABS units (kN/m) to SI (N/m).
            spring.TranslationalStiffnessX = k[0] * 1000.0;
            spring.TranslationalStiffnessY = k[1] * 1000.0;
            spring.TranslationalStiffnessZ = k[2] * 1000.0;
            spring.RotationalStiffnessX = k[3] * 1000.0;
            spring.RotationalStiffnessY = k[4] * 1000.0;
            spring.RotationalStiffnessZ = k[5] * 1000.0;

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
                // springType and hysteresisInt are updated by each link in the loop.
                // All links in a valid ETABS spring property share the same type and hysteresis,
                // so the last written value is always the correct one.
                PointSpringNonlinearType springType = PointSpringNonlinearType.MultiLinearElastic;
                int hysteresisInt = 0;

                for (int i = 0; i < nLinks; i++)
                {
                    string linkName = linkNames[i];
                    int axialDir = Math.Abs(axialDirs[i]);

                    // Confirm link is MultiLinear type.
                    eLinkPropType linkType = eLinkPropType.Linear;
                    if (m_model.PropLink.GetTypeOAPI(linkName, ref linkType) != 0)
                        continue;

                    if (linkType != eLinkPropType.MultilinearElastic && linkType != eLinkPropType.MultilinearPlastic)
                        continue;

                    springType = linkType == eLinkPropType.MultilinearElastic
                        ? PointSpringNonlinearType.MultiLinearElastic
                        : PointSpringNonlinearType.MultiLinearPlastic;

                    // Read translational curve from U1 (dof index 1).
                    int nPts = 0;
                    double[] F = null;
                    double[] D = null;
                    double a1 = 0, a2 = 0, b1 = 0, b2 = 0, eta = 0;

                    if (m_model.PropLink.GetMultiLinearPoints(
                            linkName, 1, ref nPts, ref F, ref D,
                            ref hysteresisInt, ref a1, ref a2, ref b1, ref b2, ref eta) == 0
                        && D != null && F != null)
                    {
                        List<ForceDeformationPoint> curve = new List<ForceDeformationPoint>();
                        for (int j = 0; j < Math.Min(D.Length, F.Length); j++)
                            curve.Add(new ForceDeformationPoint { Deformation = D[j], Force = F[j] * 1000.0 });

                        switch (axialDir)
                        {
                            case 1: spring.ForceDeformationCurves.TranslationX = curve; break;
                            case 2: spring.ForceDeformationCurves.TranslationY = curve; break;
                            case 3: spring.ForceDeformationCurves.TranslationZ = curve; break;
                        }
                    }

                    // Read rotational curve from R1 (dof index 4).
                    int nPtsR = 0;
                    double[] FR = null;
                    double[] DR = null;
                    double a1r = 0, a2r = 0, b1r = 0, b2r = 0, etar = 0;

                    if (m_model.PropLink.GetMultiLinearPoints(
                            linkName, 4, ref nPtsR, ref FR, ref DR,
                            ref hysteresisInt, ref a1r, ref a2r, ref b1r, ref b2r, ref etar) == 0
                        && DR != null && FR != null)
                    {
                        List<ForceDeformationPoint> rotCurve = new List<ForceDeformationPoint>();
                        for (int j = 0; j < Math.Min(DR.Length, FR.Length); j++)
                            rotCurve.Add(new ForceDeformationPoint { Deformation = DR[j], Force = FR[j] * 1000.0 });

                        switch (axialDir)
                        {
                            case 1: spring.ForceDeformationCurves.RotationX = rotCurve; break;
                            case 2: spring.ForceDeformationCurves.RotationY = rotCurve; break;
                            case 3: spring.ForceDeformationCurves.RotationZ = rotCurve; break;
                        }
                    }
                }

                // Attach ETABS-specific settings as a fragment — added once after the loop.
                spring.Fragments.Add(new PointSpringNonlinearity
                {
                    SpringType = springType,
                    SpringHysteresisType = hysteresisInt > 0 ? (HysteresisType)hysteresisInt : HysteresisType.Kinematic
                });
            }

            SetAdapterId(spring, springPropName);
            return spring;
        }

        /***************************************************/
    }
}
