/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2024, the respective contributors. All rights reserved.
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

        private List<Opening> ReadOpening(List<string> ids = null)
        {

            List<string> openingNames = new List<string>();
            List<Opening> openingList = new List<Opening>();

            int nameCount = 0;
            string[] nameArr = { };
            m_model.AreaObj.GetNameList(ref nameCount, ref nameArr);


            bool isOpening = false;
            openingNames=nameArr.Where(panelName => { m_model.AreaObj.GetOpening(panelName, ref isOpening);
                                                      return isOpening;}).ToList();

            ids = FilterIds(ids, openingNames);

            foreach (string id in ids)
            {
                ETABSId etabsId = new ETABSId();
                etabsId.Id = id;

                Opening opening = new Opening();
                Polyline pl = GetOpeningPerimeter(id);

                opening.Edges=pl.SubParts().Select(x => new Edge { Curve = x }).ToList();

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

                opening.SetAdapterId(etabsId);
                openingList.Add(opening);
            }

            return openingList;
        }

        /***************************************************/

        private Polyline GetOpeningPerimeter(string id)
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
