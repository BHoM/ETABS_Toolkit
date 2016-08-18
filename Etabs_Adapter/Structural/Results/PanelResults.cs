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
    public class PanelResults
    {
        internal static void GetPanelForces(cOAPI etabs, ResultServer<PanelForce<string, string, string>> resultServer, object p, List<string> loadcases)
        {
            //etabs.SapModel.Results.AreaForceShell()
        }
    }
}
