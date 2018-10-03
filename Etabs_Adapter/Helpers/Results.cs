using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Results;
using BH.oM.Common;
using ETABS2016;

namespace BH.Adapter.ETABS
{
    public static partial class Helper
    {
        #region Node Results

        public static List<NodeResult> GetNodeAcceleration(cSapModel model, IList ids = null, IList cases = null)
        {

            throw new NotImplementedException("Node Acceleration results is not supported yet!");
        }

        public static List<NodeDisplacement> GetNodeDisplacement(cSapModel model, IList ids = null, IList cases = null)
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
                model.PointObj.GetNameList(ref nodes, ref names);
                nodeIds = names.ToList();
            }

            if (cases == null)
            {
                int casesCount = 0;
                string[] names = null;
                model.LoadCases.GetNameList(ref casesCount, ref names);
                loadcaseIds = names.ToList();
                model.RespCombo.GetNameList(ref casesCount, ref names);
                loadcaseIds.AddRange(names);
            }

            model.Results.Setup.DeselectAllCasesAndCombosForOutput();

            for (int loadcase = 0; loadcase < loadcaseIds.Count; loadcase++)
            {
                if (model.Results.Setup.SetCaseSelectedForOutput(loadcaseIds[loadcase]) != 0)
                {
                    model.Results.Setup.SetComboSelectedForOutput(loadcaseIds[loadcase]);
                }
            }

            for (int i = 0; i < nodeIds.Count; i++)
            {
                int ret = model.Results.JointDispl(nodeIds[i].ToString(), eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
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

        public static List<NodeReaction> GetNodeReaction(cSapModel model, IList ids = null, IList cases = null)
        {
            List<string> loadcaseIds = new List<string>();
            List<string> nodeIds = new List<string>();
            List<NodeReaction> nodeReactions = new List<NodeReaction>();

            if (ids == null)
            {
                int nodes = 0;
                string[] names = null;
                model.PointObj.GetNameList(ref nodes, ref names);
                nodeIds = names.ToList();
            }

            if (cases == null)
            {
                int casesCount = 0;
                string[] names = null;
                model.LoadCases.GetNameList(ref casesCount, ref names);
                loadcaseIds = names.ToList();
                model.RespCombo.GetNameList(ref casesCount, ref names);
                loadcaseIds.AddRange(names);
            }

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


            model.Results.Setup.DeselectAllCasesAndCombosForOutput();

            for (int loadcase = 0; loadcase < loadcaseIds.Count; loadcase++)
            {
                if (model.Results.Setup.SetCaseSelectedForOutput(loadcaseIds[loadcase]) != 0)
                {
                    model.Results.Setup.SetComboSelectedForOutput(loadcaseIds[loadcase]);
                }
            }


            //List<NodeReaction<string, string, string>> nodeForces = new List<NodeReaction<string, string, string>>();
            for (int i = 0; i < nodeIds.Count; i++)
            {
                int ret = model.Results.JointReact(nodeIds[i], eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
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

        public static List<NodeResult> GetNodeVelocity(cSapModel model, IList ids = null, IList cases = null)
        {
            throw new NotImplementedException("Node Acceleration results is not supported yet!");

        }
        
        #endregion

        #region bar Results

        public static List<BarResult> GetBarDeformation(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            throw new NotImplementedException("Bar deformation results is not supported yet!");
        }

        public static List<BarForce> GetBarForce(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {
            List<string> loadcaseIds = new List<string>();
            List<string> barIds = new List<string>();
            List<BarForce> barForces = new List<BarForce>();

            if (ids == null)
            {
                int bars = 0;
                string[] names = null;
                model.FrameObj.GetNameList(ref bars, ref names);
                barIds = names.ToList();
            }

            if (cases == null)
            {
                int casesCount = 0;
                string[] names = null;
                model.LoadCases.GetNameList(ref casesCount, ref names);
                loadcaseIds = names.ToList();
                model.RespCombo.GetNameList(ref casesCount, ref names);
                loadcaseIds.AddRange(names);
            }


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


            model.Results.Setup.DeselectAllCasesAndCombosForOutput();

            for (int loadcase = 0; loadcase < loadcaseIds.Count; loadcase++)
            {
                if (model.Results.Setup.SetCaseSelectedForOutput(loadcaseIds[loadcase]) != 0)
                {
                    model.Results.Setup.SetComboSelectedForOutput(loadcaseIds[loadcase]);
                }
            }

            //List<BarForce<string, string, string>> barForces = new List<BarForce<string, string, string>>();
            int counter = 1;
            for (int i = 0; i < barIds.Count; i++)
            {
                model.FrameObj.GetOutputStations(barIds[i], ref type, ref segSize, ref divisions, ref op1, ref op2);
                int ret = model.Results.FrameForce(barIds[i], eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref objStation, ref elm, ref elmStation,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        //string step = stepType[j] != null ? stepType[j] == "Max" ? " Max" : stepType[j] == "Min" ? " Min" : "1" : "0";
                        //if (objStation[j] == 0)
                        //    counter = 1;
                        //barForces.Add(new BarForce<string, string, string>(objects[j], loadcaseNames[j], counter++, divisions, step, fx[j], fz[j], fy[j], mx[j], mz[j], my[j]));

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

        public static List<BarResult> GetBarStrain(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            throw new NotImplementedException("Bar strain results is not supported yet!");
        }

        public static List<BarResult> GetBarStress(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            throw new NotImplementedException("Bar stress results is not supported yet!");
        }


        #endregion

        #region Panel Results

        public static List<MeshForce> GetMeshForce(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {
            List<string> loadcaseIds = new List<string>();
            List<string> panelIds = new List<string>();
            List<MeshForce> meshForces = new List<MeshForce>();

            if (ids == null)
            {
                int panels = 0;
                string[] names = null;
                model.AreaObj.GetNameList(ref panels, ref names);
                panelIds = names.ToList();
            }

            if (cases == null)
            {
                int casesCount = 0;
                string[] names = null;
                model.LoadCases.GetNameList(ref casesCount, ref names);
                loadcaseIds = names.ToList();
                model.RespCombo.GetNameList(ref casesCount, ref names);
                loadcaseIds.AddRange(names);
            }

            string Name = "";
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

                int ret = model.Results.AreaForceShell(panelIds[i], eItemTypeElm.ObjectElm, ref resultCount, ref Obj, ref Elm,
                    ref PointElm, ref LoadCase, ref StepType, ref StepNum, ref F11, ref F22, ref F12, ref FMax, ref FMin, ref FAngle, ref FVM,
                    ref M11, ref M22, ref M12, ref MMax, ref MMin, ref MAngle, ref V13, ref V23, ref VMax, ref VAngle);

                for (int j = 0; j < resultCount; j++)
                {
                    MeshForce pf = new MeshForce(panelIds[i], PointElm[j], "", LoadCase[j], StepNum[j], 0, 0, 0, 
                        new oM.Geometry.CoordinateSystem(), F11[j], F22[j], F12[j], M12[j], M22[j], M12[j], V13[j], V23[j]);

                    meshForces.Add(pf);
                }
            }

            return meshForces;
        }
        
        public static List<MeshForce> GetMeshStress(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {
            throw new NotImplementedException("Panel stress results is not supported yet!");

        }


        #endregion
    }
}
