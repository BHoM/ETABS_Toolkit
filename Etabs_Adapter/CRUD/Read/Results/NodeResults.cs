/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Results;
using BH.oM.Analytical.Results;
#if Debug16 || Release16
using ETABS2016;
#elif Debug17 || Release17
using ETABSv17;
#else
using ETABSv1;
#endif
using BH.oM.Structure.Requests;
using BH.oM.Adapter;
using BH.oM.Structure.Elements;
using System.Xml.Linq;

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
        /**** Public method - Read override             ****/
        /***************************************************/

        public IEnumerable<IResult> ReadResults(NodeResultRequest request, ActionConfig actionConfig = null)
        {
            CheckAndSetUpCases(request);
            List<string> nodeIds = CheckGetNodeIds(request);

            switch (request.ResultType)
            {
                case NodeResultType.NodeReaction:
                    return ReadNodeReaction(nodeIds);
                case NodeResultType.NodeDisplacement:
                    return ReadNodeDisplacement(nodeIds);
                case NodeResultType.NodeVelocity:
                    return ReadNodeVelocity(nodeIds);
                case NodeResultType.NodeAcceleration:
                    return ReadNodeAcceleration(nodeIds);
                default:
                    Engine.Base.Compute.RecordError("Result extraction of type " + request.ResultType + " is not yet supported");
                    return new List<IResult>();
            }
        }

        /***************************************************/
        /**** Private method - Extraction methods       ****/
        /***************************************************/

        private List<NodeAcceleration> ReadNodeAcceleration(List<string> nodeIds)
        {

            List<NodeAcceleration> nodeAccelerations = new List<NodeAcceleration>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] objects = null;
            string[] elm = null;
            string[] stepType = null;
            double[] stepNum = null;
            double[] ux = null;
            double[] uy = null;
            double[] uz = null;
            double[] rx = null;
            double[] ry = null;
            double[] rz = null;

            for (int i = 0; i < nodeIds.Count; i++)
            {
                int ret = m_model.Results.JointAccAbs(nodeIds[i].ToString(), eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm, ref loadcaseNames,
                                                      ref stepType, ref stepNum, ref ux, ref uy, ref uz, ref rx, ref ry, ref rz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        int mode;
                        double timeStep;
                        GetStepAndMode(stepType[j], stepNum[j], out timeStep, out mode);
                        NodeAcceleration na = new NodeAcceleration(nodeIds[i], loadcaseNames[j], mode, timeStep, oM.Geometry.Basis.XY, ux[j], uy[j], uz[j], rx[j], ry[j], rz[j]);
                        nodeAccelerations.Add(na);
                    }
                }
            }

            return nodeAccelerations;
        }

        /***************************************************/

        private List<NodeDisplacement> ReadNodeDisplacement(List<string> nodeIds)
        {
            List<NodeDisplacement> nodeDisplacements = new List<NodeDisplacement>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] objects = null;
            string[] elm = null;
            string[] stepType = null;
            double[] stepNum = null;
            double[] ux = null;
            double[] uy = null;
            double[] uz = null;
            double[] rx = null;
            double[] ry = null;
            double[] rz = null;

            for (int i = 0; i < nodeIds.Count; i++)
            {
                int ret = m_model.Results.JointDispl(nodeIds[i].ToString(), eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
                ref loadcaseNames, ref stepType, ref stepNum, ref ux, ref uy, ref uz, ref rx, ref ry, ref rz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        int mode;
                        double timeStep;
                        GetStepAndMode(stepType[j], stepNum[j], out timeStep, out mode);
                        NodeDisplacement nd = new NodeDisplacement(nodeIds[i], loadcaseNames[j], mode, timeStep, oM.Geometry.Basis.XY, ux[j], uy[j], uz[j], rx[j], ry[j], rz[j]);
                        nodeDisplacements.Add(nd);
                    }
                }
            }

            return nodeDisplacements;

        }

        /***************************************************/

        private List<NodeReaction> ReadNodeReaction(List<string> nodeIds)
        {
            List<NodeReaction> nodeReactions = new List<NodeReaction>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] objects = null;
            string[] elm = null;
            string[] stepType = null;
            double[] stepNum = null;

            double[] fx = null;
            double[] fy = null;
            double[] fz = null;
            double[] mx = null;
            double[] my = null;
            double[] mz = null;


            for (int i = 0; i < nodeIds.Count; i++)
            {
                int ret = m_model.Results.JointReact(nodeIds[i], eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        int mode;
                        double timeStep;
                        GetStepAndMode(stepType[j], stepNum[j], out timeStep, out mode);
                        NodeReaction nr = new NodeReaction(nodeIds[i], loadcaseNames[j], mode, timeStep, oM.Geometry.Basis.XY, fx[j], fy[j], fz[j], mx[j], my[j], mz[j]);
                        nodeReactions.Add(nr);
                    }
                }
            }

            return nodeReactions;
        }

        /***************************************************/

        private List<NodeVelocity> ReadNodeVelocity(List<string> nodeIds)
        {
            List<NodeVelocity> nodeVelocities = new List<NodeVelocity>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] objects = null;
            string[] elm = null;
            string[] stepType = null;
            double[] stepNum = null;
            double[] ux = null;
            double[] uy = null;
            double[] uz = null;
            double[] rx = null;
            double[] ry = null;
            double[] rz = null;

            for (int i = 0; i < nodeIds.Count; i++)
            {
                int ret = m_model.Results.JointVelAbs(nodeIds[i], eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
                                                      ref loadcaseNames, ref stepType, ref stepNum, ref ux, ref uy, ref uz, ref rx, ref ry, ref rz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        int mode;
                        double timeStep;
                        GetStepAndMode(stepType[j], stepNum[j], out timeStep, out mode);
                        NodeVelocity nv = new NodeVelocity(nodeIds[i], loadcaseNames[j], mode, timeStep, oM.Geometry.Basis.XY, ux[j], uy[j], uz[j], rx[j], ry[j], rz[j]);
                        nodeVelocities.Add(nv);
                    }
                }
            }

            return nodeVelocities;
        }

        /***************************************************/
        /**** Private method - Support methods          ****/
        /***************************************************/

        private List<string> CheckGetNodeIds(NodeResultRequest request)
        {
            List<string> nodeIds = CheckAndGetIds<Node>(request.ObjectIds);

            if (nodeIds == null || nodeIds.Count == 0)
            {
                int nodes = 0;
                string[] names = null;
                m_model.PointObj.GetNameList(ref nodes, ref names);
                nodeIds = names.ToList();
            }
          
            return nodeIds;
        }

        /***************************************************/

    }
}







