using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM.Base.Results;
using BHoM.Structural.Results;
using ETABS2015;

namespace Etabs_Adapter.Structural.Results
{
    public class BarResults
    {
        internal static void GetBarForces(cOAPI Etabs, ResultServer<BarForce<string, string, string>> resultServer, List<string> barNumbers, List<string> loadcases, int divisions)
        {
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

            if (barNumbers == null || barNumbers.Count == 0)
            {
                int bars = 0;
                string[] names = null;
                Etabs.SapModel.FrameObj.GetNameList(ref bars, ref names);
                barNumbers = names.ToList();
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

            List<BarForce<string, string, string>> barForces = new List<BarForce<string, string, string>>();
            int counter = 1;
            for (int i = 0; i < barNumbers.Count; i++)
            {
                Etabs.SapModel.FrameObj.GetOutputStations(barNumbers[i], ref type, ref segSize, ref divisions, ref op1, ref op2);
                int ret = Etabs.SapModel.Results.FrameForce(barNumbers[i].ToString(), eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref objStation, ref elm, ref elmStation,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);
                if (ret == 0)
                {
                    for (int j = 0; j < resultCount; j++)
                    {
                        string step = stepType[j] != null ? stepType[j] == "Max" ? " Max" : stepType[j] == "Min" ? " Min" : "1" : "0";
                        if (objStation[j] == 0) counter = 1;
                        barForces.Add(new BarForce<string, string, string>(objects[j], loadcaseNames[j], counter++, divisions, step, fx[j],  fz[j], fy[j], mx[j], mz[j], my[j]));
                    }
                }
            }

            resultServer.StoreData(barForces);
        }
    }
}
