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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BH.Engine.Adapter;
using BH.oM.Adapter;
using BH.oM.Adapters.ETABS.Requests;
using BH.oM.Adapters.ETABS.Results;
using BH.oM.Analytical.Results;
using BH.oM.Base;
#if Debug16 || Release16
using ETABS2016;
#elif Debug17 || Release17
using ETABSv17;
#else
using CSiAPIv1;
#endif

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

        public IEnumerable<IResult> ReadResults(SectionCutResultRequest request, ActionConfig actionConfig = null)
        {
            // Guard against a null request before touching the API
            if (request == null)
            {
                Engine.Base.Compute.RecordError("Cannot read section cut results from a null request.");
                return new List<IResult>();
            }

            CheckAndSetUpCases(request);

            switch (request.ResultType)
            {
                case SectionCutResultType.Analysis:
                    return GetSectionCutAnalysisForce(request.ObjectIds);
                case SectionCutResultType.Design:
                    return GetSectionCutDesignForce(request.ObjectIds);
                default:
                    Engine.Base.Compute.RecordError("Result extraction of type " + request.ResultType + " is not yet supported");
                    return new List<IResult>();
            }
        }

        /***************************************************/
        /**** Private method - Extraction methods       ****/
        /***************************************************/

        /* Section Cut Analysis Forces - resultant forces in the global axes */
        private List<SectionCutForce> GetSectionCutAnalysisForce(IList ids = null)
        {
            List<SectionCutForce> sectionCutForces = new List<SectionCutForce>();

            int numberResults = 0;
            string[] sCut = null;
            string[] loadcaseNames = null;
            string[] stepType = null;
            double[] stepNum = null;

            double[] f1 = null;
            double[] f2 = null;
            double[] f3 = null;
            double[] m1 = null;
            double[] m2 = null;
            double[] m3 = null;

            int ret = m_model.Results.SectionCutAnalysis(ref numberResults, ref sCut, ref loadcaseNames, ref stepType, ref stepNum,
                                                        ref f1, ref f2, ref f3, ref m1, ref m2, ref m3);

            if (ret != 0)
            {
                Engine.Base.Compute.RecordError("Failed to extract section cut analysis forces from ETABS. Check that the analysis has been run and that section cuts are defined.");
                return sectionCutForces;
            }

            // Narrow the returned names to those requested, as the API offers no way to filter beforehand
            List<string> requestedIdList = CheckAndGetIds<IBHoMObject>(ids);
            HashSet<string> requestedIds = requestedIdList == null || requestedIdList.Count == 0 ? null : requestedIdList.ToHashSet();

            for (int i = 0; i < numberResults; i++)
            {
                if (requestedIds != null && !requestedIds.Contains(sCut[i]))
                    continue;

                SectionCutForce force = new SectionCutForce(sCut[i], loadcaseNames[i], 0, stepNum[i], stepType[i],
                                                            f1[i], f2[i], f3[i], m1[i], m2[i], m3[i]);
                sectionCutForces.Add(force);
            }

            return sectionCutForces;
        }

        /***************************************************/

        /* Section Cut Design Forces - resultant forces in the section cut local axes */
        private List<SectionCutForce> GetSectionCutDesignForce(IList ids = null)
        {
            List<SectionCutForce> sectionCutForces = new List<SectionCutForce>();

            int numberResults = 0;
            string[] sCut = null;
            string[] loadcaseNames = null;
            string[] stepType = null;
            double[] stepNum = null;

            double[] p = null;
            double[] v2 = null;
            double[] v3 = null;
            double[] t = null;
            double[] m2 = null;
            double[] m3 = null;

            int ret = m_model.Results.SectionCutDesign(ref numberResults, ref sCut, ref loadcaseNames, ref stepType, ref stepNum,
                                                      ref p, ref v2, ref v3, ref t, ref m2, ref m3);

            if (ret != 0)
            {
                Engine.Base.Compute.RecordError("Failed to extract section cut design forces from ETABS. Check that the analysis has been run and that section cuts are defined.");
                return sectionCutForces;
            }

            // Narrow the returned names to those requested, as the API offers no way to filter beforehand
            List<string> requestedIdList = CheckAndGetIds<IBHoMObject>(ids);
            HashSet<string> requestedIds = requestedIdList == null || requestedIdList.Count == 0 ? null : requestedIdList.ToHashSet();

            for (int i = 0; i < numberResults; i++)
            {
                if (requestedIds != null && !requestedIds.Contains(sCut[i]))
                    continue;

                // Map the design axes onto the BHoM force convention: P->FX, V2->FY, V3->FZ, T->MX
                SectionCutForce force = new SectionCutForce(sCut[i], loadcaseNames[i], 0, stepNum[i], stepType[i],
                                                            p[i], v2[i], v3[i], t[i], m2[i], m3[i]);
                sectionCutForces.Add(force);
            }

            return sectionCutForces;
        }

        /***************************************************/
    }
}
