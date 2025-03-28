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

        private bool CreateObject(Panel bhPanel)
        {
            bool success = true;
            int retA = 0;

            double mergeTol = 1e-3; //Merging panel points to the mm, same behaviour as the default node comparer

            if (!CheckPropertyError(bhPanel, bhP => bhP.ExternalEdges, true))
                return false;

            for (int i = 0; i < bhPanel.ExternalEdges.Count; i++)
            {
                if (!CheckPropertyError(bhPanel, bhP => bhP.ExternalEdges[i], true))
                    return false;

                if (!CheckPropertyError(bhPanel, bhP => bhP.ExternalEdges[i].Curve, true))
                    return false;
            }

            NonLinearEdgesCheck(bhPanel.ExternalEdges);

            string name = "";
            string propertyName = "";

            if(CheckPropertyWarning(bhPanel, bhP => bhP.Property))
                propertyName = GetAdapterId<string>(bhPanel.Property);

            List<BH.oM.Geometry.Point> boundaryPoints = bhPanel.ControlPoints(true).CullDuplicates(mergeTol);

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

            retA = m_model.AreaObj.AddByCoord(segmentCount, ref x, ref y, ref z, ref name, propertyName);
            ETABSId etabsid = new ETABSId();
            etabsid.Id = name;

            //Label and story
            string label = "";
            string story = "";
            string guid = null;

            if (m_model.AreaObj.GetLabelFromName(name, ref label, ref story) == 0)
            {
                etabsid.Label = label;
                etabsid.Story = story;
            }

            if (m_model.AreaObj.GetGUID(name, ref guid) == 0)
                etabsid.PersistentId = guid;

            bhPanel.SetAdapterId(etabsid);
            
            if (retA != 0)
                return false;

            if (bhPanel.Openings != null)
            {
                for (int i = 0; i < bhPanel.Openings.Count; i++)
                {

                    if (!CheckPropertyError(bhPanel, bhP => bhP.Openings[i]))
                        continue;

                    Opening opening = bhPanel.Openings[i];

                    for (int j = 0; j < opening.Edges.Count; j++)
                    {
                        if (!CheckPropertyError(opening, o => o.Edges[j], true))
                            return false;

                        if (!CheckPropertyError(opening, o => o.Edges[j], true))
                            return false;
                    }

                    NonLinearEdgesCheck(opening.Edges);

                    boundaryPoints = opening.ControlPoints().CullDuplicates(mergeTol);

                    segmentCount = boundaryPoints.Count();
                    x = new double[segmentCount];
                    y = new double[segmentCount];
                    z = new double[segmentCount];

                    for (int j = 0; j < segmentCount; j++)
                    {
                        x[j] = boundaryPoints[j].X;
                        y[j] = boundaryPoints[j].Y;
                        z[j] = boundaryPoints[j].Z;
                    }

                    string openingName = name + "_Opening_" + i;
                    m_model.AreaObj.AddByCoord(segmentCount, ref x, ref y, ref z, ref openingName, "");//<-- setting panel property to empty string, verify that this is correct
                    m_model.AreaObj.SetOpening(openingName, true);

                    SetAdapterId(bhPanel.Openings[i], openingName);
                }
            }

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
            return success;
        }

        /***************************************************/

        private static void NonLinearEdgesCheck(List<Edge> edges)
        {
            bool isNonLinear = false;

            try
            {
                isNonLinear = edges.Any(e => e.Curve.ISubParts().Any(c => !c.IIsLinear()));
            }
            catch (System.Exception)
            {
                //Try catch in case of curves not yet supported in the IsNonLinear method.
                isNonLinear = true;
            }
            if (isNonLinear)
                Engine.Base.Compute.RecordWarning("Non-linear edges will be pushed using the control points of the underlying curves. It is recomended that you subsegment all edge curves into linear segements before you push to ETABS. Try using the CollapseToPolyline method. Please check the result of the push in the ETABS model!");

        }

        /***************************************************/

    }
}





