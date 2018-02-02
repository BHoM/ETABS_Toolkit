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

            object newInstance = null;
            newInstance = System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
            int ret;

            cHelper helper = new Helper();

            app = helper.GetObject(pathToETABS);//<--get running instance (standard for adapters) else use ' helper.CreateObject(pathToETABS)' to start a new instance
            model = app.SapModel;
            
        }

    }
}
