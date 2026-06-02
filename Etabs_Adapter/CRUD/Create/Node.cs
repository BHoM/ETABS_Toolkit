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
using BH.Engine.Adapters.ETABS;
using BH.Engine.Base;
using BH.Engine.Geometry;
using BH.Engine.Structure;
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Fragments;
using BH.oM.Geometry;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Springs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

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
        private bool CreateObject(Node bhNode)
        {
            string name = "";
            ETABSId etabsid = new ETABSId();

            if (!CheckPropertyError(bhNode, x => x.Position, true))
                return false;

            oM.Geometry.Point position = bhNode.Position;
            if (m_model.PointObj.AddCartesian(position.X, position.Y, position.Z, ref name) == 0)
            {
                etabsid.Id = name;

                //Label and story
                string label = "";
                string story = "";
                if (m_model.PointObj.GetLabelFromName(name, ref label, ref story) == 0)
                {
                    etabsid.Label = label;
                    etabsid.Story = story;
                }

                string guid = null;
                if (m_model.PointObj.GetGUID(name, ref guid) == 0)
                    etabsid.PersistentId = guid;

                bhNode.SetAdapterId(etabsid);
                SetObject(bhNode, name);
                SetGroup(bhNode);
            }

            return true;
        }
        
        /***************************************************/

        private bool SetObject(Node bhNode, string name)
        {
            if (bhNode.Support != null)
            {
                bool[] restraint = new bool[6];
                double[] spring = new double[6];

                bhNode.Support.ToCSI(ref restraint, ref spring);

                if (m_model.PointObj.SetRestraint(name, ref restraint) == 0) { }
                else
                {
                    CreatePropertyWarning("Node Restraint", "Node", name);
                }
                if (m_model.PointObj.SetSpring(name, ref spring) == 0) { }
                else
                {
                    CreatePropertyWarning("Node Spring", "Node", name);
                }

            }

            if (bhNode.NonLinearSpring != null)
            {
                SetNonLinearSpring(bhNode.NonLinearSpring, name);
            }

            if (bhNode.Orientation != null && !bhNode.Orientation.IsEqual(Basis.XY))
            {
                Engine.Base.Compute.RecordWarning("ETABS does not support local coordinate systems other than the global one. Any nodes pushed will have been so as if they had the global coordinatesystem.");
            }

            return true;
        }

        private bool SetNonLinearSpring(NonLinearSpring spring, string nodeName)
        {
            string springPropName = nodeName + "_NonLinearSpring";

            // Read ETABS-specific settings from fragment
            NonLinearSpringProperties settings = spring.FindFragment<NonLinearSpringProperties>();
            NonLinearSpringType springType = settings?.SpringType ?? NonLinearSpringType.MultiLinearElastic;
            NonLinearSpringHysteresisType hysteresisType = settings?.SpringHysteresisType ?? NonLinearSpringHysteresisType.Kinematic;

            // Convert stiffness from SI (N/m, N�m/rad) to ETABS units (kN/m, kN�m/rad).
            double[] k = new double[]
            {
                spring.TranslationalStiffnessX / 1000.0,
                spring.TranslationalStiffnessY / 1000.0,
                spring.TranslationalStiffnessZ / 1000.0,
                spring.RotationalStiffnessX    / 1000.0,
                spring.RotationalStiffnessY    / 1000.0,
                spring.RotationalStiffnessZ    / 1000.0,
            };

            // Set point spring
            if (m_model.PropPointSpring.SetPointSpringProp(springPropName, 1, ref k) != 0)
                CreatePropertyWarning("NonLinear Point Spring", "Node", nodeName);

            // Group by axis direction � one link per axis, handling both translation and rotation.
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
                bool hasTranslation = translation?.Count >= 2;
                bool hasRotation = rotation?.Count >= 2;

                if (!hasTranslation && !hasRotation)
                    continue;

                string linkName = springPropName + suffix;

                // Activate U1 (index 0) for translation, R1 (index 3) for rotation.
                bool[] dof = { hasTranslation, false, false, hasRotation, false, false };
                bool[] fix = { false, false, false, false, false, false };
                bool[] nonLin = { hasTranslation, false, false, hasRotation, false, false };
                double[] stiff = { 0, 0, 0, 0, 0, 0 };
                double[] damp = { 0, 0, 0, 0, 0, 0 };

                // Set non linear links
                int ret;
                if (springType == NonLinearSpringType.MultiLinearElastic)
                {
                    ret = m_model.PropLink.SetMultiLinearElastic(linkName, ref dof, ref fix, ref nonLin, ref stiff, ref damp, 0, 0);
                }
                else
                {
                    ret = m_model.PropLink.SetMultiLinearPlastic(linkName, ref dof, ref fix, ref nonLin, ref stiff, ref damp, 0, 0);
                }

                if (ret != 0)
                {
                    CreatePropertyWarning("NonLinear Link", "Node", nodeName);
                    continue;
                }


                int hysteresisInt = springType == NonLinearSpringType.MultiLinearPlastic ? (int)hysteresisType : 0;

                // Set translational curve on U1 (dof index 1)
                if (hasTranslation)
                {
                    double[] F = translation.Select(p => p.Force / 1000.0).ToArray();
                    double[] D = translation.Select(p => p.Deformation).ToArray();

                    if (m_model.PropLink.SetMultiLinearPoints(linkName, 1, translation.Count, ref F, ref D, hysteresisInt) != 0)
                        CreatePropertyWarning("NonLinear Link Translation Points", "Node", nodeName);
                }

                // Set rotational curve on R1 (dof index 4)
                if (hasRotation)
                {
                    double[] F = rotation.Select(p => p.Force / 1000.0).ToArray();
                    double[] D = rotation.Select(p => p.Deformation).ToArray();

                    if (m_model.PropLink.SetMultiLinearPoints(linkName, 4, rotation.Count, ref F, ref D, hysteresisInt) != 0)
                        CreatePropertyWarning("NonLinear Link Rotation Points", "Node", nodeName);
                }


                linkNames.Add(linkName);
                axialDirs.Add(axialDir);
                linkAngles.Add(0.0);

            }

            // Assign non linear links on pointspring
            if (linkNames.Count > 0)
            {
                string[] linkNamesArr = linkNames.ToArray();
                int[] axialDirsArr = axialDirs.ToArray();
                double[] anglesArr = linkAngles.ToArray();

                if (m_model.PropPointSpring.SetLinks(springPropName, linkNames.Count, ref linkNamesArr, ref axialDirsArr, ref anglesArr) != 0)
                    CreatePropertyWarning("NonLinear Spring Links", "Node", nodeName);
            }

            // Assign pointspring on node
            if (m_model.PointObj.SetSpringAssignment(nodeName, springPropName) != 0)
                CreatePropertyWarning("NonLinear Spring Assignment", "Node", nodeName);

            return true;


        }

        /***************************************************/

    }
}






