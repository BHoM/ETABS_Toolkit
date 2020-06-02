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

using System.Collections.Generic;
using System.Linq;
using BH.oM.Structure.Elements;
using BH.Engine.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;

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
        /**** Update Panel                              ****/
        /***************************************************/
        
        private bool UpdateObjects(IEnumerable<Panel> bhPanels)
        {
            bool success = true;
            m_model.SelectObj.ClearSelection();

            foreach (Panel bhPanel in bhPanels)
            {
                string name = bhPanel.CustomData[AdapterIdName].ToString();
                string propertyName = bhPanel.Property.CustomData[AdapterIdName].ToString();

                Engine.Reflection.Compute.RecordWarning("The Etabs API does not allow for updating of the geometry of panels. This includes the external edges as well as the openings. To update the panel geometry, delete the existing panel you want to update and create a new one.");

                m_model.AreaObj.SetProperty(name, propertyName);

                Pier pier = bhPanel.Pier();
                Spandrel spandrel = bhPanel.Spandrel();
                Diaphragm diaphragm = bhPanel.Diaphragm();

                if (pier != null)
                {
                    int ret = m_model.PierLabel.SetPier(pier.Name);
                    ret = m_model.AreaObj.SetPier(name, pier.Name);
                }
                if (spandrel != null)
                {
                    int ret = m_model.SpandrelLabel.SetSpandrel(spandrel.Name, false);
                    ret = m_model.AreaObj.SetSpandrel(name, spandrel.Name);
                }
                if (diaphragm != null)
                {
                    m_model.AreaObj.SetDiaphragm(name, diaphragm.Name);
                }
            }

            return success;
        }

        /***************************************************/

    }
}
