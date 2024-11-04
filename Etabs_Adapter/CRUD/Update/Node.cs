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
            bool success = true;                                                               // θ(1)
            m_model.SelectObj.ClearSelection();                                                // θ(1)

            double factor = DatabaseLengthUnitFactor();                                        // θ(1)

            Engine.Structure.NodeDistanceComparer comparer = AdapterComparers[typeof(Node)]    // θ(1)
                as Engine.Structure.NodeDistanceComparer;

            Dictionary<double, List<string>> dx = new Dictionary<double, List<string>>();      // θ(1)
            Dictionary<double, List<string>> dy = new Dictionary<double, List<string>>();      // θ(1)
            Dictionary<double, List<string>> dz = new Dictionary<double, List<string>>();      // θ(1)


            // 1. GROUP NODES BY RELATIVE MOVEMENT IN X/Y/Z DIRECTION  -  ** HASH TABLES **

            foreach (Node bhNode in nodes)                                                     // n*θ(1) + θ(1)
            {
                string name = GetAdapterId<string>(bhNode);                                    // θ(1)

                // Update position
                double x = 0;                                                                  // θ(1)
                double y = 0;                                                                  // θ(1)
                double z = 0;                                                                  // θ(1)

                if (m_model.PointObj.GetCoordCartesian(name, ref x, ref y, ref z) == 0)        // θ(1)
                {
                    oM.Geometry.Point p = new oM.Geometry.Point() { X = x, Y = y, Z = z };     // θ(1)

                    if (!comparer.Equals(bhNode, (Node)p))                                     // θ(1)
                    {
                        // Get BHoM vs ETABS differences in nodes coordinates
                        x = bhNode.Position.X - x;                                             // θ(1)
                        y = bhNode.Position.Y - y;                                             // θ(1)
                        z = bhNode.Position.Z - z;                                             // θ(1)

                        // Add Node name and corresponding dX in dx Hash Table
                        if (dx.ContainsKey(x)) dx[x].Add(name);                                // θ(1)
                        else dx.Add(x, new List<string>() {name});                             // θ(1)
                        // Add Node name and corresponding dY in dy Hash Table
                        if (dy.ContainsKey(y)) dy[y].Add(name);                                // θ(1)
                        else dy.Add(y, new List<string>() {name});                             // θ(1)
                        // Add Node name and corresponding dZ in dz Hash Table
                        if (dz.ContainsKey(z)) dz[z].Add(name);                                // θ(1)
                        else dz.Add(z, new List<string>() {name});                             // θ(1)

                    }
                }
            }



            // 2. MOVE NODES GROUP-BY-GROUP  -  ** STREAMS **

            // dX Movement
            dx.ToList().ForEach(kvp =>                                                         // θ(n)
            {
                // 1. Select all nodes belonging to same group
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                // 2. Move all selected nodes by same dX
                m_model.EditGeneral.Move((double)kvp.Key, 0, 0);
                // 3. Deselect all selected nodes
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            // dY Movement
            dy.ToList().ForEach(kvp =>                                                         // θ(n)
            {
                // 1. Select all nodes belonging to same group
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                // 2. Move all selected nodes by same dY
                m_model.EditGeneral.Move(0, (double)kvp.Key, 0);
                // 3. Deselect all selected nodes
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            // dZ Movement
            dz.ToList().ForEach(kvp =>                                                         // θ(n)
            {
                // 1. Select all nodes belonging to same group
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), true));
                // 2. Move all selected nodes by same dZ
                m_model.EditGeneral.Move(0, 0, (double)kvp.Key);
                // 3. Deselect all selected nodes
                kvp.Value.ForEach(pplbl => m_model.PointObj.SetSelected(pplbl.ToString(), false));
            });

            return success;
        }

        /***************************************************/
    }
}





