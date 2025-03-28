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
using BH.oM.Structure.Elements;
using BH.oM.Structure.SurfaceProperties;
using BH.Engine.Adapters.ETABS;
using BH.oM.Geometry;
using BH.Engine.Geometry;
using BH.oM.Adapters.ETABS.Elements;
using BH.Engine.Structure;
using BH.Engine.Spatial;


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
        /***    Read Methods                             ***/
        /***************************************************/

        private List<Panel> ReadPanel(List<string> ids = null)
        {
            List<Panel> panelList = new List<Panel>();

            Dictionary<string, ISurfaceProperty> bhomProperties = GetCachedOrReadAsDictionary<string, ISurfaceProperty>();
            int nameCount = 0;
            string[] nameArr = { };
            m_model.AreaObj.GetNameList(ref nameCount, ref nameArr);

            //Remove the Opening Objects from the Panels List
            bool isOpening = false;
            List<string> nameList=nameArr.Where(panelName => {
                m_model.AreaObj.GetOpening(panelName, ref isOpening);
                return !isOpening;}).ToList();
            //Update number of Panels removing Opening Objects from it
            nameCount = nameList.Count;

            ids = FilterIds(ids, nameList);

            foreach (string id in ids)
            {
                ETABSId etabsId = new ETABSId();
                etabsId.Id = id;

                string propertyName = "";

                m_model.AreaObj.GetProperty(id, ref propertyName);

                ISurfaceProperty panelProperty = null;
                if (propertyName != "None")
                {
                    panelProperty = bhomProperties[propertyName];
                }

                Panel panel = new Panel();
                Polyline pl = GetPanelPerimeter(id);

                panel.ExternalEdges = pl.SubParts().Select(x => new Edge { Curve = x }).ToList();

                panel.Property = panelProperty;
                string PierName = "";
                string SpandrelName = "";
                m_model.AreaObj.GetPier(id, ref PierName);
                m_model.AreaObj.GetSpandrel(id, ref SpandrelName);
                panel = panel.SetSpandrel(new Spandrel { Name = SpandrelName });
                panel = panel.SetPier(new Pier { Name = PierName });

                double orientation = 0;
                bool advanced = false;
                m_model.AreaObj.GetLocalAxes(id, ref orientation, ref advanced);

                panel = panel.SetLocalOrientation(Convert.FromCSILocalX(panel.Normal(), orientation));

                //Label and story
                string label = "";
                string story = "";
                string guid = null;
                if (m_model.AreaObj.GetLabelFromName(id, ref label, ref story) == 0)
                {
                    etabsId.Label = label;
                    etabsId.Story = story;
                }

                if (m_model.AreaObj.GetGUID(id, ref guid) == 0)
                    etabsId.PersistentId = guid;

                panel.SetAdapterId(etabsId);
                panelList.Add(panel);
            }

            return panelList;
        }

        /***************************************************/

        private Polyline GetPanelPerimeter(string id)
        {
            string[] pName = null;
            int pointCount = 0;
            double pX1 = 0;
            double pY1 = 0;
            double pZ1 = 0;
            m_model.AreaObj.GetPoints(id, ref pointCount, ref pName);
            List<Point> pts = new List<Point>();
            for (int j = 0; j < pointCount; j++)
            {
                m_model.PointObj.GetCoordCartesian(pName[j], ref pX1, ref pY1, ref pZ1);
                pts.Add(new Point() { X = pX1, Y = pY1, Z = pZ1 });
            }
            pts.Add(pts[0]);

            Polyline pl = new Polyline() { ControlPoints = pts };

            return pl;
        }

        /***************************************************/

    }
}






