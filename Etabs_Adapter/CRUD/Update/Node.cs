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

using System.Collections.Generic;
using System.Linq;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Constraints;
using BH.Engine.Adapters.ETABS;
using System.Collections;
using System.Xml.Linq;

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
        /**** Update Node                               ****/
        /***************************************************/

        private bool UpdateObjects(IEnumerable<Node> nodes)
        {
            bool success = true;
            m_model.SelectObj.ClearSelection();

            double factor = DatabaseLengthUnitFactor();

            Engine.Structure.NodeDistanceComparer comparer = AdapterComparers[typeof(Node)] as Engine.Structure.NodeDistanceComparer;

            Dictionary<double, List<string>> Δx = new Dictionary<double, List<string>>();
            Dictionary<double, List<string>> Δy = new Dictionary<double, List<string>>();
            Dictionary<double, List<string>> Δz = new Dictionary<double, List<string>>();


            foreach (Node bhNode in nodes)
            {
                string name = GetAdapterId<string>(bhNode);

                SetObject(bhNode, name);

                // Update position
                double x = 0;
                double y = 0;
                double z = 0;

                if (m_model.PointObj.GetCoordCartesian(name, ref x, ref y, ref z) == 0)
                {
                    oM.Geometry.Point p = new oM.Geometry.Point() { X = x, Y = y, Z = z };

                    if (!comparer.Equals(bhNode, (Node)p))
                    {
                        x = bhNode.Position.X - x;
                        y = bhNode.Position.Y - y;
                        z = bhNode.Position.Z - z;

                        if (Δx.ContainsKey(x)) Δx[x].Add(bhNode.Name);
                        else Δx.Add(x, new List<string>());

                        if (Δy.ContainsKey(y)) Δy[y].Add(bhNode.Name);
                        else Δy.Add(y, new List<string>());

                        if (Δz.ContainsKey(z)) Δz[z].Add(bhNode.Name);
                        else Δz.Add(z, new List<string>());

                    }
                }
            }



            Δx.ToList().ForEach(kvp =>
            {
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                m_model.EditGeneral.Move((double)kvp.Key * factor, 0, 0);
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            Δy.ToList().ForEach(kvp =>
            {
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                m_model.EditGeneral.Move(0, (double)kvp.Key * factor, 0);
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            Δz.ToList().ForEach(kvp =>
            {
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                m_model.EditGeneral.Move(0, 0, (double)kvp.Key * factor);
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            return success;
        }

        /***************************************************/
    }
}





