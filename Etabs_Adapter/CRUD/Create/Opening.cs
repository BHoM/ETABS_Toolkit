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

using System.Collections.Generic;
using System.Linq;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Elements;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.Engine.Spatial;
using BH.Engine.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Geometry;
using BH.oM.Analytical.Elements;


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
        /***    Create Methods                           ***/
        /***************************************************/

        private bool CreateObject(Opening bhOpening)
        {
            bool success = true;
            int retA = 0;

            double mergeTol = 1e-3; //Merging panel points to the mm, same behaviour as the default node comparer

            if (!CheckPropertyError(bhOpening, bhO => bhO.Edges, true))
                return false;

            for (int i = 0; i < bhOpening.Edges.Count; i++)
            {
                if (!CheckPropertyError(bhOpening, bhO => bhO.Edges[i], true))
                    return false;

                if (!CheckPropertyError(bhOpening, bhO => bhO.Edges[i].Curve, true))
                    return false;
            }

            NonLinearEdgesCheck(bhOpening.Edges);

            List<BH.oM.Geometry.Point> boundaryPoints = bhOpening.ControlPoints(true).CullDuplicates(mergeTol);

            int segmentCount = boundaryPoints.Count();
            double[] x = new double[segmentCount];
            double[] y = new double[segmentCount];
            double[] z = new double[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                x[i] = boundaryPoints[i].X;
                y[i] = boundaryPoints[i].Y;
                z[i] = boundaryPoints[i].Z;
            }

            string openingName = GetAdapterId<string>(bhOpening);
            retA = m_model.AreaObj.AddByCoord(segmentCount, ref x, ref y, ref z, ref openingName, "Default");
            ETABSId etabsid = new ETABSId();
            etabsid.Id = openingName;

            //Label and story
            string label = "";
            string story = "";
            string guid = null;

            if (m_model.AreaObj.GetLabelFromName(openingName, ref label, ref story) == 0)
            {
                etabsid.Label = label;
                etabsid.Story = story;
            }

            if (m_model.AreaObj.GetGUID(openingName, ref guid) == 0)
                etabsid.PersistentId = guid;

            bhOpening.SetAdapterId(etabsid);

            m_model.AreaObj.SetOpening(openingName, true);

            return success;
        }

        /***************************************************/

    }
}
