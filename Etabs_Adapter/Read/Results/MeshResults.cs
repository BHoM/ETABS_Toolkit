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

        public IEnumerable<IResult> ReadResults(MeshResultRequest request)
        {
            CheckAndSetUpCases(request);
            List<string> panelIds = CheckGetPanelIds(request);

            Engine.Reflection.Compute.RecordWarning("The Etabs API currently does not allow you to control Smoothing, Layer and LayerPosition");
            
            switch (request.ResultType)
            {
                case MeshResultType.Forces:
                    return ReadMeshForce(panelIds, request.Smoothing);
                case MeshResultType.Displacements:
                    return ReadMeshDisplacement(panelIds);
                case MeshResultType.Stresses:
                    return ReadMeshStress(panelIds);
                case MeshResultType.VonMises:
                default:
                    Engine.Reflection.Compute.RecordError("Result extraction of type " + request.ResultType + " is not yet supported");
                    return new List<IResult>();
            }

        }

        /***************************************************/
        /**** Private method - Extraction methods       ****/
        /***************************************************/


        private List<MeshResult> ReadMeshForce(List<string> panelIds, MeshResultSmoothingType smoothing)
        {
            eItemTypeElm itemTypeElm = eItemTypeElm.ObjectElm;
            int resultCount = 0;
            string[] obj = null;
            string[] elm = null;
            string[] pointElm = null;
            string[] loadCase = null;
            string[] stepType = null;
            double[] stepNum = null;
            double[] f11 = null;
            double[] f22 = null;
            double[] f12 = null;
            double[] fMax = null;
            double[] fMin = null;
            double[] fAngle = null;
            double[] fvm = null;
            double[] m11 = null;
            double[] m22 = null;
            double[] m12 = null;
            double[] mMax = null;
            double[] mMin = null;
            double[] mAngle = null;
            double[] v13 = null;
            double[] v23 = null;
            double[] vMax = null;
            double[] vAngle = null;

            List<MeshResult> results = new List<MeshResult>();

            if (smoothing == MeshResultSmoothingType.ByPanel)
                Engine.Reflection.Compute.RecordWarning("Force values have been smoothened outside the API by summing up all force values in each node");

            for (int i = 0; i < panelIds.Count; i++)
            {

                List<MeshForce> forces = new List<MeshForce>();
                
                int ret = m_model.Results.AreaForceShell(panelIds[i], itemTypeElm, ref resultCount, ref obj, ref elm,
                    ref pointElm, ref loadCase, ref stepType, ref stepNum, ref f11, ref f22, ref f12, ref fMax, ref fMin, ref fAngle, ref fvm,
                    ref m11, ref m22, ref m12, ref mMax, ref mMin, ref mAngle, ref v13, ref v23, ref vMax, ref vAngle);

                for (int j = 0; j < resultCount; j++)
                {
                    MeshForce pf = new MeshForce(panelIds[i], pointElm[j], elm[j], loadCase[j], stepNum[j], 0, 0, 0,
                        oM.Geometry.Basis.XY, f11[j], f22[j], f12[j], m11[j], m22[j], m12[j], v13[j], v23[j]);

                    forces.Add(pf);
                }

                if (smoothing == MeshResultSmoothingType.ByPanel)
                    forces = SmoothenForces(forces);

                results.AddRange(GroupMeshResults(forces));
            }

            return results;
        }

        /***************************************************/

        private List<MeshResult> ReadMeshStress(List<string> panelIds)
        {

            eItemTypeElm itemTypeElm = eItemTypeElm.ObjectElm;
            int resultCount = 0;
            string[] obj = null;
            string[] elm = null;
            string[] pointElm = null;
            string[] loadCase = null;
            string[] stepType = null;
            double[] stepNum = null;
            double[] s11Top = null;
            double[] s22Top = null;
            double[] s12Top = null;
            double[] sMaxTop = null;
            double[] sMinTop = null;
            double[] sAngTop = null;
            double[] svmTop = null;
            double[] s11Bot = null;
            double[] s22Bot = null;
            double[] s12Bot = null;
            double[] sMaxBot = null;
            double[] sMinBot = null;
            double[] sAngBot = null;
            double[] svmBot = null;
            double[] s13Avg = null;
            double[] s23Avg = null;
            double[] sMaxAvg = null;
            double[] sAngAvg = null;

            List<MeshResult> results = new List<MeshResult>();

            for (int i = 0; i < panelIds.Count; i++)
            {
                List<MeshStress> stressTop = new List<MeshStress>();
                List<MeshStress> stressBot = new List<MeshStress>();
                int ret = m_model.Results.AreaStressShell(panelIds[i], itemTypeElm, ref resultCount, ref obj, ref elm, ref pointElm, ref loadCase, ref stepType, ref stepNum, ref s11Top, ref s22Top, ref s12Top, ref sMaxTop, ref sMinTop, ref sAngTop, ref svmTop, ref s11Bot, ref s22Bot, ref s12Bot, ref sMaxBot, ref sMinBot, ref sAngBot, ref svmBot, ref s13Avg, ref s23Avg, ref sMaxAvg, ref sAngAvg);

                for (int j = 0; j < resultCount - 1; j++)
                {
                    MeshStress mStressTop = new MeshStress(panelIds[i], pointElm[j], elm[j], loadCase[j], stepNum[j], MeshResultLayer.Upper, 1, MeshResultSmoothingType.None, oM.Geometry.Basis.XY, s11Top[j], s22Top[j], s12Top[j], s13Avg[j], s23Avg[j], sMaxTop[j], sMinTop[j], sMaxAvg[j]);
                    MeshStress mStressBot = new MeshStress(panelIds[i], pointElm[j], elm[j], loadCase[j], stepNum[j], MeshResultLayer.Lower, 0, MeshResultSmoothingType.None, oM.Geometry.Basis.XY, s11Bot[j], s22Bot[j], s12Bot[j], s13Avg[j], s23Avg[j], sMaxBot[j], sMinBot[j], sMaxAvg[j]);

                    stressBot.Add(mStressBot);
                    stressTop.Add(mStressTop);
                }

                results.AddRange(GroupMeshResults(stressBot));
                results.AddRange(GroupMeshResults(stressTop));


            }

            return results;
        }

        /***************************************************/

        private List<MeshResult> ReadMeshDisplacement(List<string> panelIds)
        {

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

            List<MeshResult> results = new List<MeshResult>();

            for (int i = 0; i < panelIds.Count; i++)
            {

                List<MeshDisplacement> displacements = new List<MeshDisplacement>();

                HashSet<string> ptNbs = new HashSet<string>();

                int nbELem = 0;
                string[] elemNames = new string[0];
                m_model.AreaObj.GetElm(panelIds[i], ref nbELem, ref elemNames);

                for (int j = 0; j < nbELem; j++)
                {
                    //Get out the name of the points for each face
                    int nbPts = 0;
                    string[] ptsNames = new string[0];
                    m_model.AreaElm.GetPoints(elemNames[j], ref nbPts, ref ptsNames);

                    foreach (string ptId in ptsNames)
                    {
                        ptNbs.Add(ptId);
                    }
                }

                foreach (string ptId in ptNbs)
                {
                    int ret = m_model.Results.JointDispl(ptId, eItemTypeElm.Element, ref resultCount, ref obj, ref elm, ref loadCase, ref stepType, ref stepNum, ref ux, ref uy, ref uz, ref rx, ref ry, ref rz);

                    for (int j = 0; j < resultCount; j++)
                    {
                        MeshDisplacement disp = new MeshDisplacement(panelIds[i], ptId, "", loadCase[j], stepNum[j], MeshResultLayer.Middle, 0, MeshResultSmoothingType.None, Basis.XY, ux[j], uy[j], uz[j], rx[j], ry[j], rz[j]);
                        displacements.Add(disp);
                    }
                }
                results.AddRange(GroupMeshResults(displacements));
            }

            return results;
        }

        /***************************************************/
        /**** Private method - Support methods          ****/
        /***************************************************/

        private List<MeshResult> GroupMeshResults(IEnumerable<MeshElementResult> meshElementResults)
        {
            List<MeshResult> results = new List<MeshResult>();
            foreach (IEnumerable<MeshElementResult> group in meshElementResults.GroupBy(x => new { x.ResultCase, x.TimeStep }))
            {
                MeshElementResult first = group.First();
                results.Add(new MeshResult(first.ObjectId, first.ResultCase, first.TimeStep, first.MeshResultLayer, first.LayerPosition, first.Smoothing, new System.Collections.ObjectModel.ReadOnlyCollection<MeshElementResult>(group.ToList())));
            }

            return results;
        }

        /***************************************************/

        private List<string> CheckGetPanelIds(MeshResultRequest request)
        {
            List<string> panelIds = new List<string>();
            var ids = request.ObjectIds;

            if (ids == null || ids.Count == 0)
            {
                int panels = 0;
                string[] names = null;
                m_model.AreaObj.GetNameList(ref panels, ref names);
                panelIds = names.ToList();
            }
            else
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    panelIds.Add(ids[i].ToString());
                }
            }

            return panelIds;
        }

        /***************************************************/

        private List<MeshForce> SmoothenForces(List<MeshForce> forces)
        {
            List<MeshForce> smoothenedForces = new List<MeshForce>();

            foreach (IEnumerable<MeshForce> group in forces.GroupBy(x => new { x.ResultCase, x.TimeStep, x.NodeId }))
            {
                MeshForce first = group.First();

                double nxx = group.Sum(x => x.NXX);
                double nyy = group.Sum(x => x.NYY);
                double nxy = group.Sum(x => x.NXY);

                double mxx = group.Sum(x => x.MXX);
                double myy = group.Sum(x => x.MYY);
                double mxy = group.Sum(x => x.MXY);

                double vx = group.Sum(x => x.VX);
                double vy = group.Sum(x => x.VY);

                smoothenedForces.Add(new MeshForce(first.ObjectId, first.NodeId, "-", first.ResultCase, first.TimeStep, first.MeshResultLayer, first.LayerPosition, MeshResultSmoothingType.ByPanel, first.Orientation, nxx, nyy, nxy, mxx, myy, mxy, vx, vy));
            }

            return smoothenedForces;
        }

        /***************************************************/

    }
}

