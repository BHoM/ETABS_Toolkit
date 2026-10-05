/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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

#if Debug16 || Release16 || Debug17 || Release17
                // ETABS 2016 and 17 are .NET Framework applications, so their API cannot be reached from .NET Core.
                if (IsNetCoreRuntime())
                {
#if Debug16 || Release16
                    BH.Engine.Base.Compute.RecordError(NetCoreUnsupportedMessage("2016"));
#else
                    BH.Engine.Base.Compute.RecordError(NetCoreUnsupportedMessage("17"));
#endif
                    return;
                }
#endif

                cHelper helper = new Helper();
                string programId = "CSI.ETABS.API.ETABSObject";


                int processes = System.Diagnostics.Process.GetProcessesByName("ETABS").Length;

                if (processes > 1)
                {
                    Engine.Base.Compute.RecordWarning("More than one ETABS instance is open. BHoM has attached to the instance that is active for the API, which by default is the one that was opened first. " +
                        "The active instance can be changed in ETABS through Tools > Active Instance for API, but you should only work with one ETABS instance at a time with BHoM.");
                }

                try
                {
                    if (processes > 0)
                    {
                        m_app = BH.Engine.Adapter.Query.GetActiveObject(programId) as cOAPI;

                        if (m_app == null)
                        {
                            BH.Engine.Base.Compute.RecordError("ETABS is running but BHoM could not attach to it. Make sure that the ETABS API is registered by running RegisterETABS.exe, found in the ETABS installation folder, as administrator.");
                            return;
                        }

                        m_model = m_app.SapModel;

                        if (!CheckVersionSupported())
                            return;

                        if (System.IO.File.Exists(filePath))
                            m_model.File.OpenFile(filePath);
                        m_model.SetPresentUnits(eUnits.N_m_C);
                    }
                    else
                    {
#if !(Debug16 || Release16 || Debug17 || Release17)
                        // Fail before starting ETABS, as versions older than 22 cannot be driven from .NET Core.
                        if (IsNetCoreRuntime() && EtabsSettings.EtabsVersion != oM.Adapters.ETABS.EtabsVersion.v22)
                        {
                            BH.Engine.Base.Compute.RecordError(NetCoreUnsupportedMessage(EtabsSettings.EtabsVersion.ToString()));
                            return;
                        }
#endif
                        if (!System.IO.File.Exists(pathToETABS))
                        {
                            BH.Engine.Base.Compute.RecordError($"No ETABS executable found at {pathToETABS}. Make sure that the EtabsVersion in the EtabsSettings matches the version of ETABS installed, or open ETABS before activating the adapter.");
                            return;
                        }

                        m_app = helper.CreateObject(pathToETABS);
                        m_app.ApplicationStart();
                        m_model = m_app.SapModel;

                        if (!CheckVersionSupported())
                            return;

                        m_model.InitializeNewModel(eUnits.N_m_C);
                        if (System.IO.File.Exists(filePath))
                            m_model.File.OpenFile(filePath);
                        else
                            m_model.File.NewBlank();
                    }

                    // Get ETABS Model FilePath
                    FilePath = m_model.GetModelFilename();

                    LoadSectionDatabaseNames();
                }
                catch (Exception e)
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to connect to ETABS: {e.Message}");
                    m_app = null;
                    m_model = null;
                }
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

        // Reads the version of the connected ETABS instance and checks that it can be driven from the current .NET runtime.
        // Records an error and releases the connection if it cannot.
        private bool CheckVersionSupported()
        {
            double doubleVer = 0;
            string version = "";
            m_model.GetVersion(ref version, ref doubleVer);
            this.EtabsVersion = version;

            Version parsed;
            if (!Version.TryParse(version, out parsed))
                return true;

            bool before227 = parsed.Major < 22 || (parsed.Major == 22 && parsed.Minor < 7);

            if (before227 && IsNetCoreRuntime())
            {
                BH.Engine.Base.Compute.RecordError(NetCoreUnsupportedMessage(version));
                m_app = null;
                m_model = null;
                return false;
            }

            if (parsed.Major == 22 && parsed.Minor < 7)
                BH.Engine.Base.Compute.RecordWarning($"ETABS {version} is not supported, as versions 22.0 to 22.6 contain errors in their API. Please update to ETABS 22.7 or later.");

            return true;
        }

        /***************************************************/

        private static bool IsNetCoreRuntime()
        {
            return Environment.Version.Major > 4;
        }

        /***************************************************/

        private static string NetCoreUnsupportedMessage(string etabsVersion)
        {
            return $"ETABS {etabsVersion} cannot be used from a .NET Core runtime, such as Rhino 8 in its default mode. " +
                   "ETABS 21 and earlier run on .NET Framework, and their API depends on .NET Remoting, which is not available in .NET Core. ETABS 22.0 to 22.6 contain errors in their API.\n" +
                   "Please use ETABS 22.7 or later, or run the adapter from a .NET Framework host. To change the runtime used by Rhino 8 to .NET Framework, follow the instructions here: https://www.rhino3d.com/en/docs/guides/netcore/";
        }

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







