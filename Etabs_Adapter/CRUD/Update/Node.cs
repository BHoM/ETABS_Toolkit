/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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

using BH.Engine.Adapter;
using BH.Engine.Adapters.ETABS;
using BH.Engine.Structure;
using BH.oM.Adapters.ETABS;
using BH.oM.Physical.Elements;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Elements;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System;
using System.IO;
using System.Linq;

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

#if !(Debug16 || Release16 || Debug17 || Release17)
            // 1. UPDATE GROUP ASSIGNMENT
            nodes.ToList().ForEach(node => UpdateGroup(node));                                 // n*θ(1) + θ(1)
#endif


            // 2. UDPATE LOCATION

            double factor = DatabaseLengthUnitFactor();                                        // θ(1)

            Engine.Structure.NodeDistanceComparer comparer = AdapterComparers[typeof(Node)]    // θ(1)
                as Engine.Structure.NodeDistanceComparer;

            // 1. GROUP NODES BY RELATIVE MOVEMENT IN X/Y/Z DIRECTION  -  ** HASH TABLES **
            Dictionary<double, List<string>> dx = new Dictionary<double, List<string>>();      // θ(1)
            Dictionary<double, List<string>> dy = new Dictionary<double, List<string>>();      // θ(1)
            Dictionary<double, List<string>> dz = new Dictionary<double, List<string>>();      // θ(1)


            // 2.1 Group Nodes by Relative Movement in X/Y/Z Direction  -  ** HASH TABLES **

            foreach (Node bhNode in nodes)                                                     // n*θ(1) + θ(1)
            {
                string name = GetAdapterId<string>(bhNode);                                    // θ(1)
                success = UpdatePosition(name, comparer, bhNode);                              // θ(1)
                // Update support before the name change below, as UpdateUniqueName renames the point.
                success = UpdateSupport(name, bhNode);                                         // θ(1)
                success = UpdateUniqueName(bhNode);                                            // θ(1)
            }

            return success;                                                                    // θ(1)
        }


        
        private bool UpdatePosition(string name, NodeDistanceComparer comparer, Node bhNode)
        {
            Dictionary<double, List<string>> dx = new Dictionary<double, List<string>>();  // θ(1)
            Dictionary<double, List<string>> dy = new Dictionary<double, List<string>>();  // θ(1)
            Dictionary<double, List<string>> dz = new Dictionary<double, List<string>>();  // θ(1)

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
                    else dx.Add(x, new List<string>() { name });                           // θ(1)
                    // Add Node name and corresponding dY in dy Hash Table
                    if (dy.ContainsKey(y)) dy[y].Add(name);                                // θ(1)
                    else dy.Add(y, new List<string>() { name });                           // θ(1)
                    // Add Node name and corresponding dZ in dz Hash Table
                    if (dz.ContainsKey(z)) dz[z].Add(name);                                // θ(1)
                    else dz.Add(z, new List<string>() { name });                           // θ(1)

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

                return true;
            }

        /***************************************************/

        // Updates the restraint and spring assignments on an existing point. Only the assignments
        // are touched here; the spring is referenced by name and its stiffness is never modified -
        // defining or modifying spring properties is handled by the spring property feature.
        private bool UpdateSupport(string name, Node bhNode)
        {
            // Default support matches ETABS' default for a new point: Ux, Uy, Uz fixed, rotations free,
            // and no spring stiffness. A null Support keeps these defaults (and the spring is cleared below).
            bool[] restraint = new bool[6] { true, true, true, false, false, false };
            double[] spring = new double[6];

            if (bhNode.Support != null)
            {
                bhNode.Support.ToCSI(ref restraint, ref spring);

            }

            // Restraints always reflect the support (SetRestraint overwrites, setting and releasing DOFs).
            if (m_model.PointObj.SetRestraint(name, ref restraint) != 0)
            {
                CreatePropertyWarning("Node Restraint", "Node", name);
            }

            // The spring is assigned by property name; its stiffness is never modified here.
            // A null Support (no support) clears any spring assignment on the point.
            string propName = bhNode.Support == null ? null : bhNode.Support.Name;

            if (String.IsNullOrEmpty(propName))
            {
                if (m_model.PointObj.DeleteSpring(name) == 0)
                {
                    return true;
                }
                else
                {
                    CreatePropertyWarning("Node Spring", "Node", name);
                    return false;
                }
            }

            // Assign by the same name the create side stores the property under (DescriptionOrName), so a
            // PointSpringProperty support re-points correctly. Creating/modifying the property itself is the
            // spring property feature's job - here we only assign a property that already exists; if not found,
            // leave the current assignment and warn.
            string assignName = bhNode.Support.DescriptionOrName();

            int numberNames = 0;
            string[] propNames = null;
            bool propExists = m_model.PropPointSpring.GetNameList(ref numberNames, ref propNames) == 0 && propNames != null && propNames.Contains(assignName);

            if (!propExists)
            {
                Engine.Base.Compute.RecordWarning($"Spring property '{assignName}' has not been created in ETABS yet; the spring assignment on node '{name}' was left unchanged.");
                return true;
            }

            if (m_model.PointObj.SetSpringAssignment(name, assignName) != 0)
                CreatePropertyWarning("Node Spring", "Node", name);

            return true;
        }

        /***************************************************/

    }
}







