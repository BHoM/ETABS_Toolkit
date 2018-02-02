using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structural.Elements;

using ETABS2016;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter : BHoMAdapter
    {

        public const string ID = "ETABS_id";
        private cOAPI app;
        private cSapModel model;

        public ETABSAdapter(string filePath = "")
        {
            AdapterId = ID;

            string pathToETABS = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "Computers and Structures", "ETABS 2016", "ETABS.exe");
            cHelper helper = new Helper();

            object runningInstance = null;
            runningInstance = System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
            if (runningInstance != null)
            {
                app = (cOAPI)runningInstance;
                model = app.SapModel;
                if (System.IO.File.Exists(filePath))
                    model.File.OpenFile(filePath);
            }
            else
            {
                //open ETABS if not running - NOTE: this behaviour is different from other adapters
                app = helper.CreateObject(pathToETABS);
                model = app.SapModel;
                model.InitializeNewModel(eUnits.kN_m_C);
                if (System.IO.File.Exists(filePath))
                    model.File.OpenFile(filePath);
                else
                    model.File.NewBlank();
            }
        }

    }
}
