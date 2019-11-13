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
                results = GetMeshResults(type, ids, cases, divisions);
            //else
            //    return new List<IResult>();

            return results;
        }

        /***************************************************/

        private IEnumerable<IResult> GetNodeResults(Type type, IList ids = null, IList cases = null)
        {
            IEnumerable<IResult> results = new List<NodeResult>();

            if (type == typeof(NodeAcceleration))
                results = GetNodeAcceleration(ids, cases);
            else if (type == typeof(NodeDisplacement))
                results = GetNodeDisplacement(ids, cases);
            else if (type == typeof(NodeReaction))
                results = GetNodeReaction(ids, cases);
            else if (type == typeof(NodeVelocity))
                results = GetNodeVelocity(ids, cases);

            return results;
        }

        /***************************************************/

        private IEnumerable<IResult> GetBarResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<BarResult> results = new List<BarResult>();

            if (type == typeof(BarDeformation))
                results = GetBarDeformation(ids, cases, divisions);
            else if (type == typeof(BarForce))
                results = GetBarForce(ids, cases, divisions);
            else if (type == typeof(BarStrain))
                results = GetBarStrain(ids, cases, divisions);
            else if (type == typeof(BarStress))
                results = GetBarStress(ids, cases, divisions);
            else if (type == typeof(PierForce))
                results = GetPierForce(ids, cases, divisions);

            return results;
        }

        /***************************************************/

        private IEnumerable<IResult> GetMeshResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<MeshElementResult> results = new List<MeshElementResult>();

            if (type == typeof(MeshForce))
                results = GetMeshForce(ids, cases, divisions);
            else if (type == typeof(MeshStress))
                results = GetMeshStress(ids, cases, divisions);

            return results;
        }

        /***************************************************/

        private List<NodeResult> GetNodeAcceleration(IList ids = null, IList cases = null)
        {

            throw new NotImplementedException("Node Acceleration results is not supported yet!");
        }

        /***************************************************/

        private List<NodeDisplacement> GetNodeDisplacement(IList ids = null, IList cases = null)
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

        private List<NodeReaction> GetNodeReaction(IList ids = null, IList cases = null)
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

        private List<NodeResult> GetNodeVelocity(IList ids = null, IList cases = null)
        {
            throw new NotImplementedException("Node Acceleration results is not supported yet!");

        }

        /***************************************************/

        private List<BarResult> GetBarDeformation(IList ids = null, IList cases = null, int divisions = 5)
        {

            throw new NotImplementedException("Bar deformation results is not supported yet!");
        }

        /***************************************************/

        private List<BarForce> GetBarForce(IList ids = null, IList cases = null, int divisions = 5)
        {

            List<string> loadcaseIds = new List<string>();
            List<string> barIds = new List<string>();
            List<BarForce> barForces = new List<BarForce>();

            if (ids == null || ids.Count == 0)
            {
                int bars = 0;
                string[] names = null;
                m_model.FrameObj.GetNameList(ref bars, ref names);
                barIds = names.ToList();
            }
            else
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    barIds.Add(ids[i].ToString());
                }
            }

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] objects = null;
            string[] elm = null;
            double[] objStation = null;
            double[] elmStation = null;
            double[] stepNum = null;
            string[] stepType = null;

            double[] fx = null;
            double[] fy = null;
            double[] fz = null;
            double[] mx = null;
            double[] my = null;
            double[] mz = null;

            int type = 0;
            double segSize = 0;
            bool op1 = false;
            bool op2 = false;


            for (int i = 0; i < barIds.Count; i++)
            {
                m_model.FrameObj.GetOutputStations(barIds[i], ref type, ref segSize, ref divisions, ref op1, ref op2);
                int ret = m_model.Results.FrameForce(barIds[i], eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref objStation, ref elm, ref elmStation,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {

                        BarForce bf = new BarForce()
                        {
                            ResultCase = loadcaseNames[j],
                            ObjectId = barIds[i],
                            MX = mx[j],
                            MY = my[j],
                            MZ = mz[j],
                            FX = fx[j],
                            FY = fy[j],
                            FZ = fz[j],
                            Divisions = divisions,
                            Position = objStation[j],
                            TimeStep = stepNum[j]
                        };

                        barForces.Add(bf);
                    }
                }
            }

            return barForces;
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
        
        /***************************************************/

        private List<BarResult> GetBarStrain(IList ids = null, IList cases = null, int divisions = 5)
        {

            throw new NotImplementedException("Bar strain results is not supported yet!");
        }

        /***************************************************/

        private List<BarResult> GetBarStress(IList ids = null, IList cases = null, int divisions = 5)
        {

            throw new NotImplementedException("Bar stress results is not supported yet!");
        }

        /***************************************************/

        private List<MeshForce> GetMeshForce(IList ids = null, IList cases = null, int divisions = 5)
        {
            List<string> loadcaseIds = new List<string>();
            List<string> panelIds = new List<string>();
            List<MeshForce> meshForces = new List<MeshForce>();

            if (ids == null)
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

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);
            
            eItemTypeElm ItemTypeElm = eItemTypeElm.ObjectElm;
	        int resultCount = 0;
	        string[] Obj = null;
	        string[] Elm = null;
            string[] PointElm = null;
            string[] LoadCase = null;
            string[] StepType = null;
            double[] StepNum = null;
            double[] F11 = null;
            double[] F22 = null;
            double[] F12 = null;
            double[] FMax = null;
            double[] FMin = null;
            double[] FAngle = null;
            double[] FVM = null;
            double[] M11 = null;
            double[] M22 = null;
            double[] M12 = null;
            double[] MMax = null;
            double[] MMin = null;
            double[] MAngle = null;
            double[] V13 = null;
            double[] V23 = null;
            double[] VMax = null;
            double[] VAngle = null;

            for (int i = 0; i < panelIds.Count; i++)
            {
                
                int ret = m_model.Results.AreaForceShell(panelIds[i], ItemTypeElm, ref resultCount, ref Obj, ref Elm,
                    ref PointElm, ref LoadCase, ref StepType, ref StepNum, ref F11, ref F22, ref F12, ref FMax, ref FMin, ref FAngle, ref FVM,
                    ref M11, ref M22, ref M12, ref MMax, ref MMin, ref MAngle, ref V13, ref V23, ref VMax, ref VAngle);

                for (int j = 0; j < resultCount; j++)
                {
                    MeshForce pf = new MeshForce(panelIds[i], PointElm[j], Elm[i], LoadCase[j], StepNum[j], 0, 0, 0, 
                        oM.Geometry.Basis.XY, F11[j], F22[j], F12[j], M12[j], M22[j], M12[j], V13[j], V23[j]);

                    meshForces.Add(pf);
                }
            }

            return meshForces;
        }

        /***************************************************/

        private List<MeshStress> GetMeshStress(IList ids = null, IList cases = null, int divisions = 5)
        {

            List<string> loadcaseIds = new List<string>();
            List<string> panelIds = new List<string>();
            List<MeshStress> meshStresses = new List<MeshStress>();

            if (ids == null)
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

            //Gets and setup all the loadcases. if cases are null or have could 0, all are assigned
            loadcaseIds = CheckAndSetUpCases(cases);
            
            eItemTypeElm ItemTypeElm = eItemTypeElm.ObjectElm;
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

            
            for (int i = 0; i < panelIds.Count; i++)
            {
                int ret = m_model.Results.AreaStressShell(panelIds[i], ItemTypeElm, ref resultCount, ref obj, ref elm, ref pointElm, ref loadCase, ref stepType, ref stepNum, ref s11Top, ref s22Top, ref s12Top, ref sMaxTop, ref sMinTop, ref sAngTop, ref svmTop, ref s11Bot, ref s22Bot, ref s12Bot, ref sMaxBot, ref sMinBot, ref sAngBot, ref svmBot, ref s13Avg, ref s23Avg, ref sMaxAvg, ref sAngAvg);
                
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        MeshStress mStressTop = new MeshStress(panelIds[i], pointElm[j], elm[j], loadCase[j], stepNum[j], MeshResultLayer.Upper, 1, MeshResultSmoothingType.None, oM.Geometry.Basis.XY, s11Top[j], s22Top[j], s12Top[j], s13Avg[j], s23Avg[j], sMaxTop[j], sMinTop[j], sMaxAvg[j]);
                        MeshStress mStressBot = new MeshStress(panelIds[i], pointElm[j], elm[j], loadCase[j], stepNum[j], MeshResultLayer.Lower, 0, MeshResultSmoothingType.None, oM.Geometry.Basis.XY, s11Bot[j], s22Bot[j], s12Bot[j], s13Avg[j], s23Avg[j], sMaxBot[j], sMinBot[j], sMaxAvg[j]);

                        meshStresses.Add(mStressBot);
                        meshStresses.Add(mStressTop);
                    }

                }
            }

            return meshStresses;
        }

        /***************************************************/
        
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

