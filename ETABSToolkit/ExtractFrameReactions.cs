using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETABSToolkit
{
    public class ExtractFrameReactions
    {
        public static BHoM.Structural.Results.Bars.BarForceCollection SolveInstance(object etabs, IEnumerable<BHoM.Structural.Bar> bars, bool activate = false)
        {
            BHoM.Structural.Results.Bars.BarForceCollection barforcecol = new BHoM.Structural.Results.Bars.BarForceCollection();

            ETABS2015.cOAPI ETABS = (ETABS2015.cOAPI)etabs;
            //Define all output arrays
            ETABS2015.eItemTypeElm ItemTypeElm = new ETABS2015.eItemTypeElm();
            int NumberResults = 20;
            string[] obj = new string[100];
            double[] objSta = new double[100];
            string[] elm = new string[100];
            double[] elmSta = new double[100];
            string[] PointElm = new string[100];
            string[] LoadCase = new string[100];
            string[] StepType = new string[100];
            double[] StepNum = new double[100];
            double[] P = new double[100];
            double[] V2 = new double[100];
            double[] V3 = new double[100];
            double[] T = new double[100];
            double[] M2 = new double[100];
            double[] M3 = new double[100];
            int ret = 5;
            int numberNames = 0;
            string[] frameList = null;
            ETABS.SapModel.FrameObj.GetNameList(ref numberNames, ref frameList);
            ret = ETABS.SapModel.Results.Setup.SetCaseSelectedForOutput("Dead");
            BHoM.Global.Project project = new BHoM.Global.Project();
            BHoM.Structural.LoadcaseFactory loadcases = project.Structure.Loadcases;
            loadcases.ForceUniqueByNumber();

               foreach (BHoM.Structural.Bar bar in bars)
               {
               ret = ETABS.SapModel.Results.FrameForce(bar.Number.ToString(), ItemTypeElm, ref NumberResults, ref obj, ref objSta, ref elm, ref elmSta, ref LoadCase, ref StepType, ref StepNum, ref P, ref V2, ref V3, ref T, ref M2, ref M3);
               for (int i = 0; i <= NumberResults-1 ; i++)
               {
                       BHoM.Structural.Results.Bars.BarForce barForce =
                       new BHoM.Structural.Results.Bars.BarForce(loadcases.Create(i, LoadCase[i]), bar.Number, i);
                       barForce.FX = P[i];
                       barForce.FY = V2[i];
                       barForce.FZ = V3[i];
                       barForce.MX = T[i];
                       barForce.MY = M2[i];
                       barForce.MZ=M3[i];
                       barForce.RelativePosition = objSta[i];
                       barforcecol.Add(barForce);
               }

               }
               return barforcecol;
               
        }
    }
}
