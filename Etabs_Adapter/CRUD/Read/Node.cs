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
using BH.oM.Analytical.Elements;
using BH.oM.Base;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Springs;
using CSiAPIv1;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;


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

        private List<Node> ReadNode(List<string> ids = null)
        {
            List<Node> nodeList = new List<Node>();

            int nameCount = 0;
            string[] nameArr = { };
            m_model.PointObj.GetNameList(ref nameCount, ref nameArr);

            ids = FilterIds(ids, nameArr);

            foreach (string id in ids)
            {
                ETABSId etabsIdFragment = new ETABSId();
                etabsIdFragment.Id = id;

                double x, y, z;
                x = y = z = 0;
                bool[] restraint = new bool[6];
                double[] spring = new double[6];

                m_model.PointObj.GetCoordCartesian(id, ref x, ref y, ref z);

                m_model.PointObj.GetRestraint(id, ref restraint);
                m_model.PointObj.GetSpring(id, ref spring);

                Constraint6DOF support = GetConstraint6DOF(restraint, spring);

                Node bhNode = new Node { Position = new oM.Geometry.Point() { X = x, Y = y, Z = z }, Support = support };


                //Label and story
                string label = "";
                string story = "";
                string guid = null;
                if (m_model.PointObj.GetLabelFromName(id, ref label, ref story) == 0)
                {
                    etabsIdFragment.Label = label;
                    etabsIdFragment.Story = story;
                }

#if !(Debug16 || Release16 || Debug17 || Release17)
                // Get the groups the bar is assigned to
                int numGroups = 0;
                string[] groupNames = new string[0];
                if (m_model.PointObj.GetGroupAssign(id, ref numGroups, ref groupNames) == 0)
                {
                    foreach (string grpName in groupNames)
                        bhNode.Tags.Add(grpName);
                }
#endif

                if (m_model.PointObj.GetGUID(id, ref guid) == 0)
                    etabsIdFragment.PersistentId = guid;

                string springPropName = "";
                if (m_model.PointObj.GetSpringAssignment(id, ref springPropName) == 0
                    && !string.IsNullOrEmpty(springPropName))
                {
                    NonLinearSpring nlSpring = GetNonLinearSpring(springPropName);
                    if (nlSpring != null)
                        bhNode.NonLinearSpring = nlSpring;
                }

                bhNode.SetAdapterId(etabsIdFragment);
                nodeList.Add(bhNode);
            }


            return nodeList;
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

        private NonLinearSpring GetNonLinearSpring(string springPropName)
        {
            // Get point spring property — confirm SpringOption = 1 (user specified).
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

            // Get attached link properties.
            int nLinks = 0;
            string[] linkNames = null;
            int[] axialDirs = null;
            double[] angles = null;

            if (m_model.PropPointSpring.GetLinks(springPropName, ref nLinks,
                    ref linkNames, ref axialDirs, ref angles) != 0
                || nLinks == 0 || linkNames == null)
                return null;

            NonLinearSpring spring = new NonLinearSpring();

            // Populate stiffness from k[] — convert ETABS units (kN/m) to SI (N/m).
            spring.TranslationalStiffnessX = k[0] * 1000.0;
            spring.TranslationalStiffnessY = k[1] * 1000.0;
            spring.TranslationalStiffnessZ = k[2] * 1000.0;
            spring.RotationalStiffnessX = k[3] * 1000.0;
            spring.RotationalStiffnessY = k[4] * 1000.0;
            spring.RotationalStiffnessZ = k[5] * 1000.0;

            // springType and hysteresisInt are updated by each link in the loop.
            // All links in a valid ETABS spring property share the same type and hysteresis,
            // so the last written value is always the correct one.
            NonLinearSpringType springType = NonLinearSpringType.MultiLinearElastic;
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
                    ? NonLinearSpringType.MultiLinearElastic
                    : NonLinearSpringType.MultiLinearPlastic;

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
            spring.Fragments.Add(new NonLinearSpringProperties
            {
                SpringType = springType,
                SpringHysteresisType = hysteresisInt > 0 ? (NonLinearSpringHysteresisType) hysteresisInt : NonLinearSpringHysteresisType.Kinematic
            });

            return spring;
        }

        /***************************************************/
    }
}







