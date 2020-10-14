/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Adapters.ETABS;
using BH.Engine.Units;
#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
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

#if Debug17 || Release17
        public ETABS17Adapter(string filePath = "", EtabsSettings etabsSetting = null, bool active = false)
#elif Debug18 || Release18
        public ETABS18Adapter(string filePath = "", EtabsSettings etabsSetting = null, bool active = false)
#else
        public ETABS2016Adapter(string filePath = "", EtabsSettings etabsSetting = null, bool active = false)
#endif
        {
            //Initialisation
            BH.Adapter.Modules.Structure.ModuleLoader.LoadModules(this);
            SetupDependencies();
            SetupComparers();
            
            if (active)
            {
                AdapterIdName = ID;

                this.EtabsSettings = etabsSetting == null ? new EtabsSettings() : etabsSetting;

                //string pathToETABS = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "Computers and Structures", "ETABS 2016", "ETABS.exe");
                //string pathToETABS = System.IO.Path.Combine("C:","Program Files", "Computers and Structures", "ETABS 2016", "ETABS.exe");
#if Debug17 || Release17
                string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 17\ETABS.exe";
                cHelper helper = new ETABSv17.Helper();
#elif Debug18 || Release18
                string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 18\ETABS.exe";
                cHelper helper = new ETABSv1.Helper();
#else
                string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 2016\ETABS.exe";
                cHelper helper = new ETABS2016.Helper();
#endif


                object runningInstance = null;
                if (System.Diagnostics.Process.GetProcessesByName("ETABS").Length > 0)
                {
                    runningInstance = System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");

                    m_app = (cOAPI)runningInstance;
                    m_model = m_app.SapModel;
                    if (System.IO.File.Exists(filePath))
                        m_model.File.OpenFile(filePath);
                    m_model.SetPresentUnits(eUnits.N_m_C);
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
                    Engine.Reflection.Compute.RecordWarning("Unknow NotApplicable unit, assumed to be meter.");
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

