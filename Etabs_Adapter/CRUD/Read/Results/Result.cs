/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
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
using BH.oM.Analytical.Results;
#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif
using BH.oM.Adapters.ETABS.Results;
using BH.oM.Structure.Loads;
using BH.oM.Data.Requests;
using BH.oM.Adapter;

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

        protected override IEnumerable<IResult> ReadResults(Type type, IList ids = null, IList cases = null, int divisions = 5, ActionConfig actionConfig = null)
        {
            //Etabs special case forces.
            //TODO: Add PierForceResultRequest
            if (type == typeof(PierForce))
                return GetPierForce(ids, cases, divisions);

            IResultRequest request = Engine.Structure.Create.IResultRequest(type, ids?.Cast<object>(), cases?.Cast<object>(), divisions);

            if (request != null)
                return this.ReadResults(request as dynamic);
            else
                return new List<IResult>();

        }

        /***************************************************/

        private List<PierForce> GetPierForce(IList ids = null, IList cases = null, int divisions = 5)
        {
            List<string> loadcaseIds = new List<string>();
            List<string> barIds = new List<string>();
            List<PierForce> pierForces = new List<PierForce>();

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);

            string[] loadcaseNames = null;

            int numberResults = 0;
            string[] storyName = null;
            string[] pierName = null;
            string[] location = null;

            double[] p = null;
            double[] v2 = null;
            double[] v3 = null;
            double[] t = null;
            double[] m2 = null;
            double[] m3 = null;


            int ret = m_model.Results.PierForce(ref numberResults, ref storyName, ref pierName, ref loadcaseNames, ref location, ref p, ref v2, ref v3, ref t, ref m2, ref m3);
            if (ret == 0)
            {
                for (int j = 0; j < numberResults; j++)
                {
                    int position = 0;
                    if (location[j].ToUpper().Contains("BOTTOM"))
                    {
                        position = 1;
                    }

                    PierForce pf = new PierForce(pierName[j], loadcaseNames[j], storyName[j], 0, 0, position, 2, p[j], v2[j], v3[j], t[j], m2[j], m3[j]);
                    pierForces.Add(pf);
                }

            }
            return pierForces;
        }


        /***************************************************/

        private List<string> CheckAndSetUpCases(IResultRequest request)
        {
            return CheckAndSetUpCases(request.Cases);
        }

        /***************************************************/

        private List<string> CheckAndSetUpCases(IList cases)
        {
            List<string> loadcaseIds = GetAllCases(cases);

            //Clear any previous case setup
            m_model.Results.Setup.DeselectAllCasesAndCombosForOutput();

            //Loop through and setup all the cases
            for (int loadcase = 0; loadcase < loadcaseIds.Count; loadcase++)
            {
                SetUpCaseOrCombo(loadcaseIds[loadcase]);
            }

            return loadcaseIds;
        }

        /***************************************************/

        private List<string> GetAllCases(IList cases)
        {
            List<string> loadcaseIds = new List<string>();

            if (cases == null || cases.Count == 0)
            {
                int Count = 0;
                string[] case_names = null;
                string[] combo_names = null;
                m_model.LoadCases.GetNameList(ref Count, ref case_names);
                m_model.RespCombo.GetNameList(ref Count, ref combo_names);
                loadcaseIds = case_names.ToList();

                if (combo_names != null)
                    loadcaseIds.AddRange(combo_names);
            }
            else
            {
                foreach (object thisCase in cases)
                {
                    if (thisCase is ICase)
                    {
                        ICase bhCase = thisCase as ICase;
                        loadcaseIds.Add(bhCase.Name.ToString());
                    }
                    else if (thisCase is string)
                    {
                        string caseId = thisCase as string;
                        loadcaseIds.Add(caseId);
                    }

                }
            }

            return loadcaseIds;
        }


        /***************************************************/

        private bool SetUpCaseOrCombo(string caseName)
        {
            // Try setting it as a Load Case
            if (m_model.Results.Setup.SetCaseSelectedForOutput(caseName) != 0)
            {
                // If that fails, try setting it as a Load Combination
                if (m_model.Results.Setup.SetComboSelectedForOutput(caseName) != 0)
                {
                    Engine.Reflection.Compute.RecordWarning("Failed to setup result extraction for case " + caseName);
                    return false;
                }
            }
            return true;
        }

        /***************************************************/

        private void GetStepAndMode(string stepType, double stepNum, out double timeStep, out int mode)
        {
            if (stepType == "Mode")
            {
                mode = (int)stepNum;
                timeStep = 0;
            }
            else
            {
                timeStep = stepNum;
                mode = 0;
            }
        }

        /***************************************************/
    }
}


