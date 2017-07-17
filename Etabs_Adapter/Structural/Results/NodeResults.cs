using BHoM.Base.Results;
using BHoM.Structural.Results;
using ETABS2016;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etabs_Adapter.Structural.Results
{
    public class NodeResults
    {
        public static bool GetNodeReactions(cOAPI Etabs, ResultServer<NodeReaction<string, string, string>> resultServer, List<string> nodeNumbers, List<string> loadcases)
        {          
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
            double gx = 0;
            double gy = 0;
            double gz = 0;

            if (nodeNumbers == null || nodeNumbers.Count == 0)
            {
                int nodes = 0;
                string[] names = null;
                Etabs.SapModel.PointObj.GetNameList(ref nodes, ref names);
                nodeNumbers = names.ToList();
            }

            if( loadcases == null || loadcases.Count == 0)
            {
                int cases = 0;
                string[] names = null;
                Etabs.SapModel.LoadCases.GetNameList(ref cases, ref names);
                loadcases = names.ToList();
                Etabs.SapModel.RespCombo.GetNameList(ref cases, ref names);
                loadcases.AddRange(names);
            }
            Etabs.SapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();

            for (int loadcase = 0; loadcase < loadcases.Count; loadcase++)
            {
                if (Etabs.SapModel.Results.Setup.SetCaseSelectedForOutput(loadcases[loadcase]) != 0)
                {
                    Etabs.SapModel.Results.Setup.SetComboSelectedForOutput(loadcases[loadcase]);
                }
            }


            List<NodeReaction<string, string, string>> nodeForces = new List<NodeReaction<string, string, string>>();
            for (int i = 0; i < nodeNumbers.Count; i++)
            {            
                int ret = Etabs.SapModel.Results.JointReact(nodeNumbers[i].ToString(), eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        string step = stepType[j] != null ? stepType[j] == "Max" ? " Max" : stepType[j] == "Min" ? " Min" : "1" : "0";
                        nodeForces.Add(new NodeReaction<string, string, string>(objects[j], loadcaseNames[j], step, fx[j], fy[j], fz[j], mx[j], my[j], mz[j]));
                    }
                }
            }

            resultServer.StoreData(nodeForces);
            return true;
        }

        internal static bool GetNodeDisplacements(cOAPI Etabs, ResultServer<NodeDisplacement<string, string, string>> resultServer, List<string> nodeNumbers, List<string> loadcases)
        {
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
            double gx = 0;
            double gy = 0;
            double gz = 0;

            if (nodeNumbers == null || nodeNumbers.Count == 0)
            {
                int nodes = 0;
                string[] names = null;
                Etabs.SapModel.PointObj.GetNameList(ref nodes, ref names);
                nodeNumbers = names.ToList();
            }

            if (loadcases == null || loadcases.Count == 0)
            {
                int cases = 0;
                string[] names = null;
                Etabs.SapModel.LoadCases.GetNameList(ref cases, ref names);
                loadcases = names.ToList();
                Etabs.SapModel.RespCombo.GetNameList(ref cases, ref names);
                loadcases.AddRange(names);
            }
            Etabs.SapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();

            for (int loadcase = 0; loadcase < loadcases.Count; loadcase++)
            {
                if (Etabs.SapModel.Results.Setup.SetCaseSelectedForOutput(loadcases[loadcase]) != 0)
                {
                    Etabs.SapModel.Results.Setup.SetComboSelectedForOutput(loadcases[loadcase]);
                }
            }
            List<NodeDisplacement<string, string, string>> nodeForces = new List<NodeDisplacement<string, string, string>>();
            for (int i = 0; i < nodeNumbers.Count; i++)
            {
                //int ret = EtabsApp.SapModel.Results.BaseReact(ref resultCount, ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, 
                //    ref mx, ref my, ref mz, ref gx, ref gy, ref gz);

                int ret = Etabs.SapModel.Results.JointDispl(nodeNumbers[i].ToString(), eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref elm,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        string step = stepType[j] != null ? stepType[j] == "Max" ? " Max" : stepType[j] == "Min" ? " Min" : "1" : "0";
                        nodeForces.Add(new NodeDisplacement<string, string, string>(objects[j], loadcaseNames[j], step, fx[j], fy[j], fz[j], mx[j], my[j], mz[j]));
                    }
                }
            }

            resultServer.StoreData(nodeForces);

            return true;
        }
    }
}
