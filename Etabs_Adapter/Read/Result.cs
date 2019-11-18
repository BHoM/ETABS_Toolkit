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

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        
        protected override IEnumerable<IResult> ReadResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<IResult> results = new List<IResult>();

            if (typeof(StructuralGlobalResult).IsAssignableFrom(type))
                results = GetGlobalResults(type, cases);
            else
                results = GetObjectResults(type, ids, cases, divisions);

            return results;
        }

        /***************************************************/

        private IEnumerable<IResult> GetGlobalResults(Type type, IList cases)
        {
            if (typeof(GlobalReactions).IsAssignableFrom(type))
                return GetGlobalReactions(cases);
            if (typeof(ModalDynamics).IsAssignableFrom(type))
                return GetModalParticipationMassRatios(cases);

            return new List<IResult>();

        }

        /***************************************************/

        private IEnumerable<IResult> GetObjectResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<IResult> results = new List<IResult>();

            if (typeof(NodeResult).IsAssignableFrom(type))
                results = GetNodeResults(type, ids, cases);
            else if (typeof(BarResult).IsAssignableFrom(type))
                results = GetBarResults(type, ids, cases, divisions);
            else if (typeof(MeshElementResult).IsAssignableFrom(type))
                results = GetMeshResults(type, ids, cases);
            //else
            //    return new List<IResult>();

            return results;
        }

        /***************************************************/

        private IEnumerable<IResult> GetNodeResults(Type type, IList ids = null, IList cases = null)
        {
            IEnumerable<IResult> results = new List<NodeResult>();

            if (type == typeof(NodeAcceleration))
                results = ReadNodeAcceleration(ids, cases);
            else if (type == typeof(NodeDisplacement))
                results = ReadNodeDisplacement(ids, cases);
            else if (type == typeof(NodeReaction))
                results = ReadNodeReaction(ids, cases);
            else if (type == typeof(NodeVelocity))
                results = ReadNodeVelocity(ids, cases);

            return results;
        }

        /***************************************************/

        private IEnumerable<IResult> GetBarResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<BarResult> results = new List<BarResult>();

            if (type == typeof(BarDeformation))
                results = ReadBarDeformation(ids, cases, divisions);
            else if (type == typeof(BarForce))
                results = ReadBarForce(ids, cases, divisions);
            else if (type == typeof(BarStrain))
                results = ReadBarStrain(ids, cases, divisions);
            else if (type == typeof(BarStress))
                results = ReadBarStress(ids, cases, divisions);
            else if (type == typeof(PierForce))
                results = GetPierForce(ids, cases, divisions);

            return results;
        }

        /***************************************************/

        private IEnumerable<IResult> GetMeshResults(Type type, IList ids = null, IList cases = null)
        {
            IEnumerable<MeshElementResult> results = new List<MeshElementResult>();

            if (type == typeof(MeshForce))
                results = ReadMeshForce(ids, cases);
            else if (type == typeof(MeshStress))
                results = ReadMeshStress(ids, cases);

            return results;
        }

        /***************************************************/

       

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
                    PierForce bf = new PierForce()
                    {
                        ResultCase = loadcaseNames[j],
                        ObjectId = pierName[j],
                        MX = t[j],
                        MY = m2[j],
                        MZ = m3[j],
                        FX = p[j],
                        FY = v2[j],
                        FZ = v3[j],
                        //Divisions = divisions,
                        Position = position,
                        // TimeStep = stepNum[j]
                    };
                    bf.Location = storyName[j];
                    pierForces.Add(bf);
                }

            }
            return pierForces;
        }
        


        
        private List<GlobalReactions> GetGlobalReactions(IList cases = null)
        {
            List<string> loadcaseIds = new List<string>();
            List<GlobalReactions> globalReactions = new List<GlobalReactions>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] stepType = null; double[] stepNum = null;
            double[] fx = null; double[] fy = null; double[] fz = null;
            double[] mx = null; double[] my = null; double[] mz = null;
            double gx = 0; double gy = 0; double gz = 0;

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);

            m_model.Results.BaseReact(ref resultCount, ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, ref gx, ref gy, ref gz);

            for (int i = 0; i < resultCount; i++)
            {
                GlobalReactions g = new GlobalReactions()
                {
                    ResultCase = loadcaseNames[i],
                    FX = fx[i],
                    FY = fy[i],
                    FZ = fz[i],
                    MX = mx[i],
                    MY = my[i],
                    MZ = mz[i],
                    TimeStep = stepNum[i]
                };

                globalReactions.Add(g);
            }

            return globalReactions;
        }

        /***************************************************/

        private List<ModalDynamics> GetModalParticipationMassRatios(IList cases = null)
        {
            List<string> loadcaseIds = new List<string>();

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);

            List<ModalDynamics> partRatios = new List<ModalDynamics>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] stepType = null; double[] stepNum = null;
            double[] period = null;
            double[] ux = null; double[] uy = null; double[] uz = null;
            double[] sumUx = null; double[] sumUy = null; double[] sumUz = null;
            double[] rx = null; double[] ry = null; double[] rz = null;
            double[] sumRx = null; double[] sumRy = null; double[] sumRz = null;

            int res = m_model.Results.ModalParticipatingMassRatios(ref resultCount, ref loadcaseNames, ref stepType, ref stepNum,
                ref period, ref ux, ref uy, ref uz, ref sumUx, ref sumUy, ref sumUz, ref rx, ref ry, ref rz, ref sumRx, ref sumRy, ref sumRz);

            if (res != 0) Engine.Reflection.Compute.RecordError("Could not extract Modal information.");


            // Although API documentation says that StepNumber should correspond to the Mode Number, testing shows that StepNumber is always 0.
            string previousModalCase = "";
            int modeNumber = 1; //makes up for stepnumber always = 0
            for (int i = 0; i < resultCount; i++)
            {
                if (loadcaseNames[i] != previousModalCase)
                    modeNumber = 1;

                ModalDynamics mod = new ModalDynamics()
                {
                    ResultCase = loadcaseNames[i],
                    ModeNumber = modeNumber,
                    Frequency = 1 / period[i],
                    MassRatioX = ux[i],
                    MassRatioY = uy[i],
                    MassRatioZ = uz[i],
                    InertiaRatioX = rx[i],
                    InertiaRatioY = ry[i],
                    InertiaRatioZ = rz[i]
                };

                modeNumber += 1;
                previousModalCase = loadcaseNames[i];

                partRatios.Add(mod);
            }

            return partRatios;
        }
        
        /***************************************************/

        private List<string> CheckAndSetUpCases(IList cases)
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

            //Clear any previous case setup
            m_model.Results.Setup.DeselectAllCasesAndCombosForOutput();

            //Loop through and setup all the cases
            for (int loadcase = 0; loadcase < loadcaseIds.Count; loadcase++)
            {
                // Try setting it as a Load Case
                if (m_model.Results.Setup.SetCaseSelectedForOutput(loadcaseIds[loadcase]) != 0)
                {
                    // If that fails, try setting it as a Load Combination
                    if (m_model.Results.Setup.SetComboSelectedForOutput(loadcaseIds[loadcase]) != 0)
                    {
                        Engine.Reflection.Compute.RecordWarning("Failed to setup result extraction for case " + loadcaseIds[loadcase]);
                    }
                }
            }

            return loadcaseIds;
        }

        /***************************************************/
    }
}

