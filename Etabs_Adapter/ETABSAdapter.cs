/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
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
using ETABSv1;
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

        public EtabsSettings EtabsSettings { get; set; } = new EtabsSettings();

        /***************************************************/
        /**** Constructors                              ****/
        /***************************************************/


        [Input("filePath","Optional file path. If empty, or not a valid file path. If empty, a new file will be created unless ETABS is already running.",typeof(FilePathAttribute))]
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
            BH.Adapter.Modules.Structure.ModuleLoader.LoadModules(this);
            SetupDependencies();
            SetupComparers();
            AdapterIdFragmentType = typeof(ETABSId);

            if (active)
            {
                this.EtabsSettings = etabsSetting == null ? new EtabsSettings() : etabsSetting;

#if Debug16 || Release16
                string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 2016\ETABS.exe";

#elif Debug17 || Release17
                string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 17\ETABS.exe";
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
                    object runningInstance = System.Runtime.InteropServices.Marshal.GetActiveObject(programId);

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
                    m_app = helper.CreateObjectProgID(programId); //Starts the latest installed version of ETABS
#endif
                    m_app.ApplicationStart();
                    m_model = m_app.SapModel;
                    m_model.InitializeNewModel(eUnits.N_m_C);
                    if (System.IO.File.Exists(filePath))
                        m_model.File.OpenFile(filePath);
                    else
                        m_model.File.NewBlank();
                }

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
            //Forcing refresh of the model by moving all elements in back and forward along the x-axis.
            //If a more elegant way can be found to do this, this should be changed.
            m_model.SelectObj.All();
            m_model.EditGeneral.Move(1, 0, 0);
            m_model.SelectObj.All();
            m_model.EditGeneral.Move(-1, 0, 0);
            m_model.SelectObj.ClearSelection();
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



