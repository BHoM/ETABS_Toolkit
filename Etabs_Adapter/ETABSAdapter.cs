using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structural.Elements;
using BH.Engine.ETABS;
using ETABS2016;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter : BHoMAdapter
    {

        public const string ID = "ETABS_id";
        private cOAPI app;
        private cSapModel model;//deprecated !!!
        private ModelData modelData;
        
        public ETABSAdapter(string filePath = "")
        {
            AdapterId = ID;

            Config.SeparateProperties = true;
            Config.MergeWithComparer = true;
            Config.ProcessInMemory = false;
            Config.CloneBeforePush = true;

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
                model.SetPresentUnits(eUnits.kN_m_C);
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

            modelData = new ModelData(model);
        }

    }
}
