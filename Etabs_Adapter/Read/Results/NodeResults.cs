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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Results;
using BH.oM.Common;
#if Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS.Elements;
using BH.Engine.ETABS;
using BH.oM.Structure.Loads;
using BH.oM.Structure.Requests;
using BH.oM.Geometry;
using BH.Engine.Geometry;

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public method - Read override             ****/
        /***************************************************/

        public IEnumerable<IResult> ReadResults(NodeResultRequest request)
        {
            switch (request.ResultType)
            {
                case NodeResultType.NodeReaction:
                    return ReadNodeReaction(request.ObjectIds, request.Cases);
                case NodeResultType.NodeDisplacement:
                    return ReadNodeDisplacement(request.ObjectIds, request.Cases);
                case NodeResultType.NodeVelocity:
                case NodeResultType.NodeAcceleration:
                default:
                    Engine.Reflection.Compute.RecordError("Result extraction of type " + request.ResultType + " is not yet supported");
                    return new List<IResult>();
            }
        }

        /***************************************************/
        /**** Private method - Extraction methods       ****/
        /***************************************************/

        private List<NodeResult> ReadNodeAcceleration(IList ids = null, IList cases = null)
        {

            throw new NotImplementedException("Node Acceleration results is not supported yet!");
        }

        /***************************************************/

        private List<NodeDisplacement> ReadNodeDisplacement(IList ids = null, IList cases = null)
        {

            List<string> loadcaseIds = new List<string>();
            List<string> nodeIds = new List<string>();
            List<NodeDisplacement> nodeDisplacements = new List<NodeDisplacement>();

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

            if (ids == null)
            {
                int nodes = 0;
                string[] names = null;
                m_model.PointObj.GetNameList(ref nodes, ref names);
                nodeIds = names.ToList();
            }
            else
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    nodeIds.Add(ids[i].ToString());
                }
            }

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);

            for (int i = 0; i < nodeIds.Count; i++)
            {
                int ret = m_model.Results.JointDispl(nodeIds[i].ToString(), eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        //string step = stepType[j] != null ? stepType[j] == "Max" ? " Max" : stepType[j] == "Min" ? " Min" : "1" : "0";
                        //nodeForces.Add(new NodeDisplacement<string, string, string>(objects[j], loadcaseNames[j], step, fx[j], fy[j], fz[j], mx[j], my[j], mz[j]));

                        NodeDisplacement nd = new NodeDisplacement()
                        {
                            ResultCase = loadcaseNames[j],
                            ObjectId = nodeIds[i],
                            RX = mx[j],
                            RY = my[j],
                            RZ = mz[j],
                            UX = fx[j],
                            UY = fy[j],
                            UZ = fz[j],
                            TimeStep = stepNum[j]
                        };
                        nodeDisplacements.Add(nd);
                    }
                }
            }

            return nodeDisplacements;

        }

        /***************************************************/

        private List<NodeReaction> ReadNodeReaction(IList ids = null, IList cases = null)
        {
            List<string> loadcaseIds = new List<string>();
            List<string> nodeIds = new List<string>();
            List<NodeReaction> nodeReactions = new List<NodeReaction>();

            if (ids == null)
            {
                int nodes = 0;
                string[] names = null;
                m_model.PointObj.GetNameList(ref nodes, ref names);
                nodeIds = names.ToList();
            }
            else
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    nodeIds.Add(ids[i].ToString());
                }
            }

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);

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



            //List<NodeReaction<string, string, string>> nodeForces = new List<NodeReaction<string, string, string>>();
            for (int i = 0; i < nodeIds.Count; i++)
            {
                int ret = m_model.Results.JointReact(nodeIds[i], eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        //string step = stepType[j] != null ? stepType[j] == "Max" ? " Max" : stepType[j] == "Min" ? " Min" : "1" : "0";
                        //nodeForces.Add(new NodeReaction<string, string, string>(objects[j], loadcaseNames[j], step, fx[j], fy[j], fz[j], mx[j], my[j], mz[j]));
                        NodeReaction nr = new NodeReaction()
                        {
                            ResultCase = loadcaseNames[j],
                            ObjectId = nodeIds[i],
                            MX = mx[j],
                            MY = my[j],
                            MZ = mz[j],
                            FX = fx[j],
                            FY = fy[j],
                            FZ = fz[j],
                            TimeStep = stepNum[j]
                        };
                        nodeReactions.Add(nr);
                    }
                }
            }

            return nodeReactions;
        }

        /***************************************************/

        private List<NodeResult> ReadNodeVelocity(IList ids = null, IList cases = null)
        {
            throw new NotImplementedException("Node Acceleration results is not supported yet!");

        }
        /***************************************************/

    }
}

