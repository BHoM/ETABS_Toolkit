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
            List<string> cases = GetAllCases(request.Cases);
            CheckAndSetUpCases(request);
            List<string> panelIds = CheckGetPanelIds(request);

            
            switch (request.ResultType)
            {
                case MeshResultType.Forces:
                    return ReadMeshForce(panelIds, request.Smoothing);
                case MeshResultType.Displacements:
                    return ReadMeshDisplacement(panelIds, request.Smoothing);
                case MeshResultType.Stresses:
                    return ReadMeshStress(panelIds, cases, request.Smoothing, request.Layer);
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

            switch (smoothing)
            {
                case MeshResultSmoothingType.BySelection:
                case MeshResultSmoothingType.Global:
                case MeshResultSmoothingType.ByFiniteElementCentres:
                    Engine.Reflection.Compute.RecordWarning("Smoothing type not supported for MeshForce. No results extracted");
                    return new List<MeshResult>();
            }

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
                Engine.Reflection.Compute.RecordWarning("Force values have been smoothened outside the API by averaging all force values in each node");

            for (int i = 0; i < panelIds.Count; i++)
            {

                List<MeshForce> forces = new List<MeshForce>();
                
                int ret = m_model.Results.AreaForceShell(panelIds[i], itemTypeElm, ref resultCount, ref obj, ref elm,
                    ref pointElm, ref loadCase, ref stepType, ref stepNum, ref f11, ref f22, ref f12, ref fMax, ref fMin, ref fAngle, ref fvm,
                    ref m11, ref m22, ref m12, ref mMax, ref mMin, ref mAngle, ref v13, ref v23, ref vMax, ref vAngle);

                for (int j = 0; j < resultCount; j++)
                {
                    double step = 0;
                    if (stepType[j] == "Single Value" || stepNum.Length < j)
                        step = 0;
                    else
                        step = stepNum[j];

                    MeshForce pf = new MeshForce(panelIds[i], pointElm[j], elm[j], loadCase[j], step, 0, 0, 0,
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

        private List<MeshResult> ReadMeshStress(List<string> panelIds, List<string> cases, MeshResultSmoothingType smoothing, MeshResultLayer layer)
        {
            switch (smoothing)
            {
                case MeshResultSmoothingType.BySelection:
                case MeshResultSmoothingType.Global:
                case MeshResultSmoothingType.ByFiniteElementCentres:
                    Engine.Reflection.Compute.RecordWarning("Smoothing type not supported for MeshStress. No results extracted");
                    return new List<MeshResult>();
            }

            if (layer == MeshResultLayer.Upper || layer == MeshResultLayer.Lower)
            {
                Engine.Reflection.Compute.RecordWarning("Results for both bot and top layers will be extracted at the same time");
            }
            else
            {
                Engine.Reflection.Compute.RecordWarning("Stress extraction is currently only possible at bot and top layers. Please update the MeshResultLayer parameter");
                return new List<MeshResult>();
            }

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


            if (smoothing == MeshResultSmoothingType.ByPanel)
                Engine.Reflection.Compute.RecordWarning("Stress values have been smoothened outside the API by averaging all force values in each node");

            foreach (string caseName in cases)
            {
                m_model.Results.Setup.DeselectAllCasesAndCombosForOutput();
                if (!SetUpCaseOrCombo(caseName))
                    continue;

                for (int i = 0; i < panelIds.Count; i++)
                {

                    List<MeshStress> stressTop = new List<MeshStress>();
                    List<MeshStress> stressBot = new List<MeshStress>();
                    int ret = m_model.Results.AreaStressShell(panelIds[i], itemTypeElm, ref resultCount, ref obj, ref elm, ref pointElm, ref loadCase, ref stepType, ref stepNum, ref s11Top, ref s22Top, ref s12Top, ref sMaxTop, ref sMinTop, ref sAngTop, ref svmTop, ref s11Bot, ref s22Bot, ref s12Bot, ref sMaxBot, ref sMinBot, ref sAngBot, ref svmBot, ref s13Avg, ref s23Avg, ref sMaxAvg, ref sAngAvg);

                    if (ret == 0)
                    {

                        for (int j = 0; j < resultCount; j++)
                        {
                            MeshStress mStressTop = new MeshStress(panelIds[i], pointElm[j], elm[j], loadCase[j], stepNum[j], MeshResultLayer.Upper, 1, MeshResultSmoothingType.None, oM.Geometry.Basis.XY, s11Top[j], s22Top[j], s12Top[j], s13Avg[j], s23Avg[j], sMaxTop[j], sMinTop[j], double.NaN);
                            MeshStress mStressBot = new MeshStress(panelIds[i], pointElm[j], elm[j], loadCase[j], stepNum[j], MeshResultLayer.Lower, 0, MeshResultSmoothingType.None, oM.Geometry.Basis.XY, s11Bot[j], s22Bot[j], s12Bot[j], s13Avg[j], s23Avg[j], sMaxBot[j], sMinBot[j], double.NaN);

                            stressBot.Add(mStressBot);
                            stressTop.Add(mStressTop);
                        }


                        if (smoothing == MeshResultSmoothingType.ByPanel)
                        {
                            stressTop = SmoothenStresses(stressTop);
                            stressBot = SmoothenStresses(stressBot);
                        }

                        results.AddRange(GroupMeshResults(stressBot));
                        results.AddRange(GroupMeshResults(stressTop));

                    }
                    else
                    {
                        Engine.Reflection.Compute.RecordWarning("Failed to extract results for element " + panelIds[i] + " for case " + caseName);
                    }
                }
            }

            return results;
        }

        /***************************************************/

        //Method atempting to extract results using AreaStressLayered method. API call is currently never returning any results for this.
        //Keeping for further reference. Method is not called from anywhere
        private List<MeshResult> ReadMeshStressLayered(List<string> panelIds, MeshResultSmoothingType smoothing, List<string> cases)
        {
            switch (smoothing)
            {
                case MeshResultSmoothingType.BySelection:
                case MeshResultSmoothingType.Global:
                case MeshResultSmoothingType.ByFiniteElementCentres:
                    Engine.Reflection.Compute.RecordWarning("Smoothing type not supported for MeshStress. No results extracted");
                    return new List<MeshResult>();
            }

            eItemTypeElm itemTypeElm = eItemTypeElm.ObjectElm;
            int resultCount = 0;
            string[] obj = null;
            string[] elm = null;
            string[] layer = null;

            int[] intPtNb = null;
            double[] layerPos = null;

            string[] pointElm = null;
            string[] loadCase = null;
            string[] stepType = null;
            double[] stepNum = null;
            double[] s11 = null;
            double[] s22 = null;
            double[] s12 = null;
            double[] sMax = null;
            double[] sMin = null;
            double[] sAng = null;
            double[] svm = null;

            double[] s13 = null;
            double[] s23 = null;
            double[] sMaxAvg = null;
            double[] sAngAvg = null;

            List<MeshResult> results = new List<MeshResult>();


            if (smoothing == MeshResultSmoothingType.ByPanel)
                Engine.Reflection.Compute.RecordWarning("Stress values have been smoothened outside the API by averaging all force values in each node");


            foreach (string caseName in cases)
            {
                m_model.Results.Setup.DeselectAllCasesAndCombosForOutput();
                if (!SetUpCaseOrCombo(caseName))
                    continue;

                for (int i = 0; i < panelIds.Count; i++)
                {

                    List<MeshStress> stresses = new List<MeshStress>();
                    int ret = m_model.Results.AreaStressShellLayered(panelIds[i], itemTypeElm, ref resultCount, ref obj, ref elm, ref layer, ref intPtNb, ref layerPos, ref pointElm, ref loadCase, ref stepType, ref stepNum, ref s11, ref s22, ref s12, ref sMax, ref sMin, ref sAng, ref svm, ref s13, ref s23, ref sMaxAvg, ref sAngAvg);
                    
                    if (ret == 0)
                    {

                        for (int j = 0; j < resultCount - 1; j++)
                        {
                            MeshStress mStress = new MeshStress(panelIds[i], pointElm[j], elm[j], loadCase[j], stepNum[j], MeshResultLayer.Arbitrary, layerPos[j], MeshResultSmoothingType.None, oM.Geometry.Basis.XY, s11[j], s22[j], s12[j], s13[j], s23[j], sMax[j], sMin[j], sMaxAvg[j]);

                            stresses.Add(mStress);
                        }


                        if (smoothing == MeshResultSmoothingType.ByPanel)
                        {
                            stresses = SmoothenStresses(stresses);
                        }

                        results.AddRange(GroupMeshResults(stresses));
                    }
                    else
                    {
                        Engine.Reflection.Compute.RecordWarning("Failed to extract results for element " + panelIds[i] + " for case " + caseName);
                    }

                }
            }

            return results;
        }

        /***************************************************/

        private List<MeshResult> ReadMeshDisplacement(List<string> panelIds, MeshResultSmoothingType smoothing)
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

                double nxx = group.Average(x => x.NXX);
                double nyy = group.Average(x => x.NYY);
                double nxy = group.Average(x => x.NXY);

                double mxx = group.Average(x => x.MXX);
                double myy = group.Average(x => x.MYY);
                double mxy = group.Average(x => x.MXY);

                double vx = group.Average(x => x.VX);
                double vy = group.Average(x => x.VY);

                smoothenedForces.Add(new MeshForce(first.ObjectId, first.NodeId, "", first.ResultCase, first.TimeStep, first.MeshResultLayer, first.LayerPosition, MeshResultSmoothingType.ByPanel, first.Orientation, nxx, nyy, nxy, mxx, myy, mxy, vx, vy));
            }

            return smoothenedForces;
        }

        /***************************************************/

        private List<MeshStress> SmoothenStresses(List<MeshStress> forces)
        {
            List<MeshStress> smoothenedForces = new List<MeshStress>();

            foreach (IEnumerable<MeshStress> group in forces.GroupBy(x => new { x.ResultCase, x.TimeStep, x.NodeId }))
            {
                MeshStress first = group.First();

                double sxx = group.Average(x => x.SXX);
                double syy = group.Average(x => x.SYY);
                double sxy = group.Average(x => x.SXY);

                double txx = group.Average(x => x.TXX);
                double tyy = group.Average(x => x.TYY);

                double pr1 = group.Average(x => x.Principal_1);
                double pr2 = group.Average(x => x.Principal_2);
                double pr1_2 = group.Average(x => x.Principal_1_2);

                smoothenedForces.Add(new MeshStress(first.ObjectId, first.NodeId, "", first.ResultCase, first.TimeStep, first.MeshResultLayer, first.LayerPosition, MeshResultSmoothingType.ByPanel, first.Orientation, sxx, syy, sxy, txx, tyy, pr1, pr2, pr1_2));
            }

            return smoothenedForces;
        }

        /***************************************************/

    }
}

