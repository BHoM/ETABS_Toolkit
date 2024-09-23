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
using BH.oM.Physical.Elements;

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


            // 1. GROUP NODES BY RELATIVE MOVEMENT IN X/Y/Z DIRECTION

            foreach (Node bhNode in nodes)
            {
                string name = GetAdapterId<string>(bhNode);

                // Update position
                double x = 0;
                double y = 0;
                double z = 0;

                if (m_model.PointObj.GetCoordCartesian(name, ref x, ref y, ref z) == 0)
                {
                    oM.Geometry.Point p = new oM.Geometry.Point() { X = x, Y = y, Z = z };

                    if (!comparer.Equals(bhNode, (Node)p))
                    {
                        // Get BHoM vs ETABS differences in nodes coordinates
                        x = bhNode.Position.X - x;
                        y = bhNode.Position.Y - y;
                        z = bhNode.Position.Z - z;

                        // Add Node name and corresponding ΔX in Δx Hash Table
                        if (Δx.ContainsKey(x)) Δx[x].Add(name);
                        else Δx.Add(x, new List<string>() {name});
                        // Add Node name and corresponding ΔY in Δy Hash Table
                        if (Δy.ContainsKey(y)) Δy[y].Add(name);
                        else Δy.Add(y, new List<string>() {name});
                        // Add Node name and corresponding ΔZ in Δz Hash Table
                        if (Δz.ContainsKey(z)) Δz[z].Add(name);
                        else Δz.Add(z, new List<string>() {name});

                    }
                }
            }



            // 2. MOVE NODES GROUP-BY-GROUP

            // ΔX Movement
            Δx.ToList().ForEach(kvp =>
            {
                // 1. Select all nodes belonging to same group
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                // 2. Move all selected nodes by same ΔX
                m_model.EditGeneral.Move((double)kvp.Key, 0, 0);
                // 3. Deselect all selected nodes
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            // ΔY Movement
            Δy.ToList().ForEach(kvp =>
            {
                // 1. Select all nodes belonging to same group
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                // 2. Move all selected nodes by same ΔY
                m_model.EditGeneral.Move(0, (double)kvp.Key, 0);
                // 3. Deselect all selected nodes
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            // ΔZ Movement
            Δz.ToList().ForEach(kvp =>
            {
                // 1. Select all nodes belonging to same group
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                // 2. Move all selected nodes by same ΔZ
                m_model.EditGeneral.Move(0, 0, (double)kvp.Key);
                // 3. Deselect all selected nodes
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            return success;
        }

        /***************************************************/
    }
}





