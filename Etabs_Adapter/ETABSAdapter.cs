/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.Engine.Units;
#if Debug16 || Release16
using ETABS2016;
#elif Debug17 || Release17
using ETABSv17;
#else
using CSiAPIv1;
#endif

namespace BH.Adapter.ETABS
{
#if Debug16 || Release16
    [Description("Class for handling connection to ETABS version 2016.")]
    public partial class ETABS2016Adapter : BHoMAdapter
#elif Debug17 || Release17
    [Description("Class for handling connection to ETABS version 17.")]
   public partial class ETABS17Adapter : BHoMAdapter
#else
    [Description("Class for handling connection to ETABS version 18 and later.")]
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public Properties                         ****/
        /***************************************************/

        public const string ID = "ETABS_id";

        public string FilePath { get; set; }
        public string EtabsVersion { get; set; }

        public EtabsSettings EtabsSettings { get; set; } = new EtabsSettings();

        /***************************************************/
        /**** Constructors                              ****/
        /***************************************************/


        [Input("filePath", "Optional file path. If empty, or not a valid file path. If empty, a new file will be created unless ETABS is already running.", typeof(FilePathAttribute))]
        [Input("etabsSetting", "Controling various settings of the adapter.")]
        [Input("active", "Toggle to true to activate the adapter. If ETABS is running, the adapter will connect to the running instance. If ETABS is not running, the adapter will start up a new instance of ETABS.")]
#if Debug16 || Release16
       [Description("Creates an adapter to ETABS 2016. For connection to ETABS v18 or later use the ETABSAdapter.  For connection to ETABS v17 use the ETABS17Adapter. Earlier versions not supported.")]
        public ETABS2016Adapter(string filePath = "", EtabsSettings etabsSetting = null, bool active = false)
#elif Debug17 || Release17
        [Description("Creates an adapter to ETABS v17. For connection to ETABS v18 or later use the ETABSAdapter. For connection to ETABS 2016 use the ETABS2016Adapter. Earlier versions not supported.")]
        public ETABS17Adapter(string filePath = "", EtabsSettings etabsSetting = null, bool active = false)
#else
        [Description("Creates an adapter to ETABS version 18 or later. For connection to ETABS v17 use the ETABS17Adapter. For connection to ETABS 2016 use the ETABS2016Adapter. Earlier versions not supported.")]
        public ETABSAdapter(string filePath = "", EtabsSettings etabsSetting = null, bool active = false)
#endif
        {
            if (Environment.Version.Major > 4)
            {
                BH.Engine.Base.Compute.RecordError($"The ETABSAdapter is currently not supported in net runtimes above NETFramework due to internal errors in the ETABS API. A fix for this is being worked on.\n" +
                                                   $"If you are running this Adapter from Grasshopper in Rhino 8, you can change the runtime being used by Rhino to NETFramework. To do this please follow the instructions here: https://www.rhino3d.com/en/docs/guides/netcore/");
                return;
            }

            //Initialisation
            AdapterIdFragmentType = typeof(ETABSId);
            BH.Adapter.Modules.Structure.ModuleLoader.LoadModules(this);
            SetupDependencies();
            SetupPriorities();
            SetupComparers();
            m_AdapterSettings.HandlePriorities = true;
            m_AdapterSettings.DefaultPushType = oM.Adapter.PushType.CreateNonExisting;


            if (active)
            {
                this.EtabsSettings = etabsSetting == null ? new EtabsSettings() : etabsSetting;

#if Debug16 || Release16
                string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 2016\ETABS.exe";

#elif Debug17 || Release17
                string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 17\ETABS.exe";

#else
                string pathToETABS = "";

                switch (EtabsSettings.EtabsVersion)
                {
                    case oM.Adapters.ETABS.EtabsVersion.v18:
                        pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 18\ETABS.exe";
                        break;
                    case oM.Adapters.ETABS.EtabsVersion.v20:
                        pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 20\ETABS.exe";
                        break;
                    case oM.Adapters.ETABS.EtabsVersion.v21:
                        pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 21\ETABS.exe";
                        break;
                    case oM.Adapters.ETABS.EtabsVersion.v22:
                        pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 22\ETABS.exe";
                        break;
                    default:
                        pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 22\ETABS.exe";
                        break;
                }
#endif


                cHelper helper = new Helper();
                string programId = "CSI.ETABS.API.ETABSObject";


                int processes = System.Diagnostics.Process.GetProcessesByName("ETABS").Length;

                if (processes > 1)
                {
                    Engine.Base.Compute.RecordWarning("More than one ETABS instance is open. BHoM has attached to the most recently updated process, " +
                        "but you should only work with one ETABS instance at a time with BHoM.");
                }

                if (processes > 0)
                {
                    object runningInstance = Query.GetActiveObject(programId);

                    m_app = (cOAPI)runningInstance;
                    m_model = m_app.SapModel;
                    if (System.IO.File.Exists(filePath))
                        m_model.File.OpenFile(filePath);
                    m_model.SetPresentUnits(eUnits.N_m_C);
                }
                else
                {
#if Debug16 || Release16 || Debug17 || Release17
                    m_app = helper.CreateObject(pathToETABS);
#else
                    m_app = helper.CreateObject(pathToETABS);
#endif
                    m_app.ApplicationStart();
                    m_model = m_app.SapModel;
                    m_model.InitializeNewModel(eUnits.N_m_C);
                    if (System.IO.File.Exists(filePath))
                        m_model.File.OpenFile(filePath);
                    else
                        m_model.File.NewBlank();
                }

                // Get ETABS Model Version
                double doubleVer = 0;
                string version = "";
                m_app.SapModel.GetVersion(ref version, ref doubleVer);
                this.EtabsVersion = version;

                // Get ETABS Model FilePath
                FilePath = m_model.GetModelFilename();

                LoadSectionDatabaseNames();
            }
        }

        /***************************************************/
        /**** Private Fields                            ****/
        /***************************************************/

        private cOAPI m_app;
        private cSapModel m_model;
        private string[] m_DBSectionsNames;

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private bool ForceRefresh()
        {
            m_model.View.RefreshView();
            m_model.View.RefreshWindow();
            return true;
        }

        private void LoadSectionDatabaseNames()
        {
            int num = 0;
            eFramePropType[] types = null;
            if (EtabsSettings.DatabaseSettings.SectionDatabase != SectionDatabase.None)
            {
                m_model.PropFrame.GetPropFileNameList(
                    ToEtabsFileName(EtabsSettings.DatabaseSettings.SectionDatabase),
                    ref num, ref m_DBSectionsNames, ref types);
            }
        }

        /***************************************************/

        public double DatabaseLengthUnitFactor()
        {
            eForce force = 0;
            eLength length = 0;
            eTemperature temp = 0;

            m_model.GetDatabaseUnits_2(ref force, ref length, ref temp);

            double factor = 1;

            switch (length)
            {
                case eLength.NotApplicable:
                    Engine.Base.Compute.RecordWarning("Unknow NotApplicable unit, assumed to be meter.");
                    factor = 1;
                    break;
                case eLength.inch:
                    factor = factor.ToInch();
                    break;
                case eLength.ft:
                    factor = factor.ToFoot();
                    break;
                case eLength.micron:
                    factor = factor.ToMicrometre();
                    break;
                case eLength.mm:
                    factor = factor.ToMillimetre();
                    break;
                case eLength.cm:
                    factor = factor.ToCentimetre();
                    break;
                case eLength.m:
                    factor = 1;
                    break;
                default:
                    break;
            }

            return factor;
        }

        /***************************************************/

    }
}






