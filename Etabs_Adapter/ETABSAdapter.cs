using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;
using ETABS2016;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter : BHoMAdapter
    {

        /***************************************************/
        /**** Public Properties                         ****/
        /***************************************************/

        public const string ID = "ETABS_id";

        public EtabsConfig EtabsConfig { get; set; } = new EtabsConfig();

        /***************************************************/
        /**** Constructors                              ****/
        /***************************************************/

        public ETABSAdapter(string filePath = "", EtabsConfig etabsConfig = null, bool active = false)
        {
            if (active)
            {
                AdapterId = ID;

                this.EtabsConfig = etabsConfig == null ? new EtabsConfig() : etabsConfig;
                
                Config.SeparateProperties = true;
                Config.MergeWithComparer = true;
                Config.ProcessInMemory = false;
                Config.CloneBeforePush = true;

                //string pathToETABS = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "Computers and Structures", "ETABS 2016", "ETABS.exe");
                //string pathToETABS = System.IO.Path.Combine("C:","Program Files", "Computers and Structures", "ETABS 2016", "ETABS.exe");
                string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 2016\ETABS.exe";
                cHelper helper = new ETABS2016.Helper();

                object runningInstance = null;
                if (System.Diagnostics.Process.GetProcessesByName("ETABS").Length > 0)
                {
                    runningInstance = System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");

                    m_app = (cOAPI)runningInstance;
                    m_model = m_app.SapModel;
                    if (System.IO.File.Exists(filePath))
                        m_model.File.OpenFile(filePath);
                    m_model.SetPresentUnits(eUnits.kip_ft_F);
                }
                else
                {
                    //open ETABS if not running - NOTE: this behaviour is different from other adapters
                    m_app = helper.CreateObject(pathToETABS);
                    m_app.ApplicationStart();
                    m_model = m_app.SapModel;
                    m_model.InitializeNewModel(eUnits.N_m_C);
                    if (System.IO.File.Exists(filePath))
                        m_model.File.OpenFile(filePath);
                    else
                        m_model.File.NewBlank();
                }

            }

        }

        /***************************************************/
        /**** Private Fields                            ****/
        /***************************************************/

        private cOAPI m_app;
        private cSapModel m_model;

        /***************************************************/

    }
}
