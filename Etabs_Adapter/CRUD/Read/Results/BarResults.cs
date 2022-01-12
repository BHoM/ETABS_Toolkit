/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
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
#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif
using BH.oM.Structure.Requests;
using BH.oM.Geometry;
using BH.Engine.Geometry;
using BH.oM.Adapter;
using BH.oM.Structure.Elements;

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public method - Read override             ****/
        /***************************************************/

        public IEnumerable<IResult> ReadResults(BarResultRequest request, ActionConfig actionConfig = null)
        {
            CheckAndSetUpCases(request);
            List<string> barIds = CheckGetBarIds(request);

            switch (request.ResultType)
            {
                case BarResultType.BarForce:
                    return ReadBarForce(barIds, request.Divisions);
                case BarResultType.BarDisplacement:
                    return ReadBarDisplacements(barIds, request.Divisions);
                case BarResultType.BarDeformation:
                    Engine.Base.Compute.RecordError("Etabs cannot export localised BarDeformations. To get the full displacement of the bars in global coordinates, try pulling BarDisplacements");
                    return new List<IResult>();
                case BarResultType.BarStress:
                case BarResultType.BarStrain:
                default:
                    Engine.Base.Compute.RecordError("Result extraction of type " + request.ResultType + " is not yet supported");
                    return new List<IResult>();
            }
        }

        /***************************************************/
        /**** Private method - Extraction methods       ****/
        /***************************************************/


        private List<BarForce> ReadBarForce(List<string> barIds, int divisions)
        {
            List<BarForce> barForces = new List<BarForce>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] objects = null;
            string[] elm = null;
            double[] objStation = null;
            double[] elmStation = null;
            double[] stepNum = null;
            string[] stepType = null;

            double[] p = null;
            double[] v2 = null;
            double[] v3 = null;
            double[] t = null;
            double[] m2 = null;
            double[] m3 = null;

            int type = 2; //Use minimum nb of division points
            double segSize = 0;
            bool op1 = false;
            bool op2 = false;

            Dictionary<string, Point> points = new Dictionary<string, Point>();

            for (int i = 0; i < barIds.Count; i++)
            {
                //Get element length
                double length = GetBarLength(barIds[i], points);

                int divs = divisions;

                m_model.FrameObj.SetOutputStations(barIds[i], type, 0, divs);
                m_model.FrameObj.GetOutputStations(barIds[i], ref type, ref segSize, ref divs, ref op1, ref op2);

                int ret = m_model.Results.FrameForce(barIds[i], eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref objStation, ref elm, ref elmStation,
                ref loadcaseNames, ref stepType, ref stepNum, ref p, ref v2, ref v3, ref t, ref m2, ref m3);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        int mode;
                        double timeStep;
                        GetStepAndMode(stepType[j], stepNum[j], out timeStep, out mode);

                        BarForce bf = new BarForce(barIds[i], loadcaseNames[j], mode, timeStep, objStation[j] / length, divs, p[j], v3[j], v2[j], t[j], -m3[j], m2[j]);
                        barForces.Add(bf);
                    }
                }
            }

            return barForces;
        }

        /***************************************************/

        private List<BarDisplacement> ReadBarDisplacements(List<string> barIds, int divisions)
        {
            List<BarDisplacement> displacements = new List<BarDisplacement>();

            Engine.Base.Compute.RecordWarning("Displacements will only be extracted at ETABS calculation nodes. 'Divisions' parameter will not be considered in result extraction");

            int resultCount = 0;
            string[] obj = null;
            string[] elm = null;
            string[] loadCase = null;
            string[] stepType = null;
            double[] stepNum = null;

            double[] ux = null;
            double[] uy = null;
            double[] uz = null;
            double[] rx = null;
            double[] ry = null;
            double[] rz = null;

            Dictionary<string, Point> points = new Dictionary<string, Point>();

            for (int i = 0; i < barIds.Count; i++)
            {
                int divs = divisions;
                string[] intElem = null;
                double[] di = null;
                double[] dj = null;

                m_model.FrameObj.GetElm(barIds[i], ref divs, ref intElem, ref di, ref dj);

                Dictionary<string, double> nodeWithPos = new Dictionary<string, double>();

                for (int j = 0; j < divs; j++)
                {
                    string p1Id = "";
                    string p2Id = "";
                    m_model.LineElm.GetPoints(intElem[j], ref p1Id, ref p2Id);

                    nodeWithPos[p1Id] = di[j];
                    nodeWithPos[p2Id] = dj[j];
                }


                foreach (var nodePos in nodeWithPos)
                {
                    int ret = m_model.Results.JointDispl(nodePos.Key, eItemTypeElm.Element, ref resultCount, ref obj, ref elm, ref loadCase, ref stepType, ref stepNum, ref ux, ref uy, ref uz, ref rx, ref ry, ref rz);
                    
                    if (ret == 0)
                    {
                        for (int j = 0; j < resultCount; j++)
                        {
                            int mode;
                            double timeStep;
                            GetStepAndMode(stepType[j], stepNum[j], out timeStep, out mode);

                            BarDisplacement disp = new BarDisplacement(barIds[i], loadCase[j], mode, timeStep, nodePos.Value, divs + 1, ux[j], uy[j], uz[j], rx[j], ry[j], rz[j]);
                            displacements.Add(disp);
                        }
                    }

                }

            }

            return displacements;
        }


        /***************************************************/

        private List<BarResult> ReadBarStrain(IList ids = null, IList cases = null, int divisions = 5)
        {

            throw new NotImplementedException("Bar strain results are not supported yet!");
        }

        /***************************************************/

        private List<BarResult> ReadBarStress(IList ids = null, IList cases = null, int divisions = 5)
        {

            throw new NotImplementedException("Bar stress results are not supported yet!");
        }

        /***************************************************/
        /**** Private method - Support methods          ****/
        /***************************************************/

        private List<string> CheckGetBarIds(BarResultRequest request)
        {
            List<string> barIds = CheckAndGetIds<Bar>(request.ObjectIds);

            if (barIds == null || barIds.Count == 0)
            {
                int bars = 0;
                string[] names = null;
                m_model.FrameObj.GetNameList(ref bars, ref names);
                barIds = names.ToList();
            }
            return barIds;
        }

        /***************************************************/

        private double GetBarLength(string barId, Dictionary<string, Point> pts)
        {
            string p1Id = "";
            string p2Id = "";

            m_model.FrameObj.GetPoints(barId, ref p1Id, ref p2Id);

            Point p1 = CheckGetPoint(p1Id, pts);
            Point p2 = CheckGetPoint(p2Id, pts);

            return p1.Distance(p2);
        }

        /***************************************************/

        private Point CheckGetPoint(string pointId, Dictionary<string, Point> pts)
        {
            Point pt;
            double x = 0;
            double y = 0;
            double z = 0;
            if (!pts.TryGetValue(pointId, out pt))
            {
                m_model.PointObj.GetCoordCartesian(pointId, ref x, ref y, ref z);
                pt = new Point() { X = x, Y = y, Z = z };
                pts[pointId] = pt;
            }
            return pt;
        }

        /***************************************************/
    }
}




