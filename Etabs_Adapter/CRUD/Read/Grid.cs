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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Geometry.SettingOut;
using BH.oM.Geometry;
using System.ComponentModel;
#if Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/

        private List<Grid> ReadGrid(List<string> ids = null)
        {
            List<Grid> gridList = new List<Grid>();
            int NumberNames = 0;
            string[] Names = null;

            m_model.GridSys.GetNameList(ref NumberNames, ref Names);
            List<string> gridSystemId = Names.ToList();
            
            foreach (string id in gridSystemId)
            {
                // Each GridSystem has an Id in ETABS
                gridList.AddRange(CartesianGrids(id, ids));
            }

            return gridList;
        }

        /***************************************************/

        [Description("Gets all the grids in a Carteasian Grid System")]
        private List<Grid> CartesianGrids(string id, List<string> ids)
        {
            double xO = 0;
            double yO = 0;
            double rZ = 0;
            int numXLines = 0;
            string[] gridLineIDX = null;
            double[] ordinateX = null;
            bool[] visibleX = null;
            string[] bubbleLocX = null;
            int numYLines = 0;
            string[] gridLineIDY = null;
            double[] ordinateY = null;
            bool[] visibleY = null;
            string[] bubbleLocY = null;
            string gridSysType = "";

            if (m_model.GridSys.GetGridSys_2(
                id, ref xO, ref yO, ref rZ, ref gridSysType,
                ref numXLines, ref numYLines,
                ref gridLineIDX, ref gridLineIDY,
                ref ordinateX, ref ordinateY,
                ref visibleX, ref visibleY,
                ref bubbleLocX, ref bubbleLocY) == 1)
                return new List<Grid>();
            if (gridSysType != "Cartesian")
            {
                Engine.Reflection.Compute.RecordWarning("Can only pull Cartesian Grid systems from ETABS, offender: " + id);
                return new List<Grid>();
            }

            // Get bounds
            double minY = ordinateY.Min();
            double maxY = ordinateY.Max();

            double minX = ordinateX.Min();
            double maxX = ordinateX.Max();

            // Cull based on ids if present
            if (ids != null)
            {
                ordinateX = ordinateX.Where((value, i) => ids.Contains(gridLineIDX[i])).ToArray();
                ordinateY = ordinateY.Where((value, i) => ids.Contains(gridLineIDY[i])).ToArray();
            }

            // Create Lines in each orientation
            List<Line> result = ordinateX.Select(x => new Line()
                {
                    Start = new Point() { X = x, Y = minY },
                    End = new Point() { X = x, Y = maxY }
                }
            ).ToList();
            result.AddRange(ordinateY.Select(y => new Line()
                {
                    Start = new Point() { X = minX, Y = y },
                    End = new Point() { X = maxX, Y = y }
                }
            ));

            // Place at gridsystem origin and orientation
            result = result.Select(x => Engine.Geometry.Modify.Rotate(x, Point.Origin, Vector.ZAxis, rZ * Math.PI / 180)).ToList();
            Vector origin = new Vector() { X = xO, Y = yO };
            result = result.Select(x => Engine.Geometry.Modify.Translate(x, origin)).ToList();

            return result.Select(x => new Grid() { Curve = x }).ToList();
        }

        /***************************************************/

    }
}

