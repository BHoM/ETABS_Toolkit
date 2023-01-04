/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2023, the respective contributors. All rights reserved.
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
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Elements;
using BH.Engine.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Adapter;
using BH.oM.Geometry;
using BH.Engine.Structure;

namespace BH.Adapter.ETABS
{
#if Debug16 || Release16
    public partial class ETABS2016Adapter : BHoMAdapter
#elif Debug17 || Release17
   public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Update Panel                              ****/
        /***************************************************/
        
        private bool UpdateObjects(IEnumerable<Panel> bhPanels)
        {
            //Make sure Diaphragms are pushed
            List<Diaphragm> diaphragms = bhPanels.Select(x => x.Diaphragm()).Where(x => x != null).ToList();
            this.FullCRUD(diaphragms, PushType.FullPush);

            bool success = true;
            m_model.SelectObj.ClearSelection();

            foreach (Panel bhPanel in bhPanels)
            {
                string name = GetAdapterId<string>(bhPanel);
                string propertyName = GetAdapterId<string>(bhPanel.Property);

                Engine.Base.Compute.RecordWarning("The Etabs API does not allow for updating of the geometry of panels. This includes the external edges as well as the openings. To update the panel geometry, delete the existing panel you want to update and create a new one.");

                m_model.AreaObj.SetProperty(name, propertyName);

                //Set local orientations:
                Basis orientation = bhPanel.LocalOrientation();
                m_model.AreaObj.SetLocalAxes(name, Convert.ToEtabsPanelOrientation(orientation.Z, orientation.Y));

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

            //Force refresh to make sure panel local orientation are set correctly
            ForceRefresh();

            return success;
        }

        /***************************************************/

    }
}



