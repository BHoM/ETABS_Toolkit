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
using BH.oM.Adapters.ETABS.Results;
using BH.oM.Structure.Loads;
using BH.oM.Data.Requests;
using BH.oM.Structure.Requests;
using BH.oM.Adapters.ETABS.Requests;
using BH.oM.Adapter;
using BH.oM.Base;

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
        /**** Private method - Support methods          ****/
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

        private void ReadResultsError(Type resultType)
        {
            Type requestType = null;

            if (typeof(PierForce).IsAssignableFrom(resultType))
                requestType = typeof(PierAndSpandrelForceRequest);
            else if (typeof(BarResult).IsAssignableFrom(resultType))
                requestType = typeof(BarResultRequest);
            else if (typeof(MeshResult).IsAssignableFrom(resultType) || typeof(MeshElementResult).IsAssignableFrom(resultType))
                requestType = typeof(MeshResultRequest);
            else if (typeof(StructuralGlobalResult).IsAssignableFrom(resultType))
                requestType = typeof(GlobalResultRequest);
            else if (typeof(NodeResult).IsAssignableFrom(resultType))
                requestType = typeof(NodeResultRequest);

            Modules.Structure.ErrorMessages.ReadResultsError(resultType, requestType);
        }

        /***************************************************/

        private List<string> CheckAndGetIds<T>(IEnumerable ids) where T : IBHoMObject
        {
            if (ids == null)
            {
                return null;
            }
            else
            {
                List<string> idsOut = new List<string>();
                foreach (object o in ids)
                {
                    if (o is string)
                        idsOut.Add((string)o);
                    else if (o is int || o is double)
                        idsOut.Add(o.ToString());
                    else if (o is T)
                    {
                        string id = GetAdapterId<string>((T)o);
                        if (id != null)
                            idsOut.Add(id);
                    }
                }
                return idsOut;
            }
        }

        /***************************************************/
    }
}




