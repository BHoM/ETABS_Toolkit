/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
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
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;
#if Debug18 || Release18
using ETABSv1;
#elif Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug18 || Release18
    public partial class ETABS18Adapter : BHoMAdapter
#elif Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public Properties                         ****/
        /***************************************************/

        public const string ID = "ETABS_id";

        public EtabsConfig EtabsConfig { get; set; } = new EtabsConfig();

        /***************************************************/
        /**** Constructors                              ****/
        /***************************************************/

#if Debug18 || Release18
        public ETABS18Adapter(string filePath = "", EtabsConfig etabsConfig = null, bool active = false)
#elif Debug17 || Release17
        public ETABS17Adapter(string filePath = "", EtabsConfig etabsConfig = null, bool active = false)
#else
        public ETABS2016Adapter(string filePath = "", EtabsConfig etabsConfig = null, bool active = false)
#endif
        {
            if (active)
            {
                AdapterId = ID;

                this.EtabsConfig = etabsConfig == null ? new EtabsConfig() : etabsConfig;

                Config.SeparateProperties = true;
                Config.MergeWithComparer = true;
                Config.ProcessInMemory = false;
                Config.CloneBeforePush = true;


                cHelper helper = new Helper();

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
                    m_app = helper.CreateObjectProgID("CSI.ETABS.API.ETABSObject");
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
