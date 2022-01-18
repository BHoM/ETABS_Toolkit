/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
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
using BH.oM.Spatial.SettingOut;
using BH.oM.Geometry;
using System.ComponentModel;
using BH.Engine.Geometry;


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

        private List<Grid> ReadGrid(List<string> ids = null)
        {
            List<Grid> gridList = new List<Grid>();
            int NumberNames = 0;
            string[] Names = null;

            m_model.GridSys.GetNameList(ref NumberNames, ref Names);
            List<string> gridSystemId = Names.ToList();

            foreach (string id in gridSystemId)
            {
                string gridSysType = "";
                // Each GridSystem has an Id in ETABS
                m_model.GridSys.GetGridSysType(id, ref gridSysType);
                switch (gridSysType)
                {
                    case "Cartesian":
                        gridList.AddRange(CartesianGrids(id, ids));
                        break;
                    case "Cylindrical":
                        gridList.AddRange(CylindricalGrids(id, ids));
                        break;
                    default:
                        Engine.Base.Compute.RecordWarning("Can not pull " + gridSysType + " Grid systems from ETABS, offender: " + id);
                        break;
                }
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

            bool storyRangedefault = true;
            string top = "";
            string bottom = "";
            double bubbleSize = 0;
            int gridColour = -1;

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

            int numGLines = 0;
            string[] gridLineIDG = null;
            double[] ordinateGX1 = null;
            double[] ordinateGY1 = null;
            double[] ordinateGX2 = null;
            double[] ordinateGY2 = null;
            bool[] visibleG = null;
            string[] bubbleLocG = null;

            if (m_model.GridSys.GetGridSysCartesian(
                id, ref xO, ref yO, ref rZ,
                ref storyRangedefault, ref top, ref bottom,
                ref bubbleSize, ref gridColour,
                ref numXLines, ref gridLineIDX, ref ordinateX, ref visibleX, ref bubbleLocX,
                ref numYLines, ref gridLineIDY, ref ordinateY, ref visibleY, ref bubbleLocY,
                ref numGLines, ref gridLineIDG,
                    ref ordinateGX1, ref ordinateGY1, ref ordinateGX2, ref ordinateGY2,
                ref visibleG, ref bubbleLocG) == 1)
            {
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
                gridLineIDX = gridLineIDX.Where(x => ids.Contains(x)).ToArray();
                gridLineIDY = gridLineIDY.Where(x => ids.Contains(x)).ToArray();

                ordinateGX1 = ordinateGX1.Where((value, i) => ids.Contains(gridLineIDG[i])).ToArray();
                ordinateGY1 = ordinateGY1.Where((value, i) => ids.Contains(gridLineIDG[i])).ToArray();
                ordinateGX2 = ordinateGX2.Where((value, i) => ids.Contains(gridLineIDG[i])).ToArray();
                ordinateGY2 = ordinateGY2.Where((value, i) => ids.Contains(gridLineIDG[i])).ToArray();
                gridLineIDG = gridLineIDG.Where(x => ids.Contains(x)).ToArray();
            }

            // Format the names like the Lines
            List<string> names = gridLineIDX.Concat(gridLineIDY).Concat(gridLineIDG).ToList();

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
            result.AddRange(gridLineIDG.Select((identificator, index) =>
           {
               return new Line()
               {
                   Start = new Point() { X = ordinateGX1[index], Y = ordinateGY1[index] },
                   End = new Point() { X = ordinateGX2[index], Y = ordinateGY2[index] }
               };
           }));

            // Place at gridsystem origin and orientation
            result = result.Select(x => Engine.Geometry.Modify.Rotate(x, Point.Origin, Vector.ZAxis, rZ * Math.PI / 180)).ToList();
            Vector origin = new Vector() { X = xO, Y = yO };
            result = result.Select(x => Engine.Geometry.Modify.Translate(x, origin)).ToList();

            return result.Zip(names, (x, name) => new Grid() { Curve = x, Name = name }).ToList();


        }

        /***************************************************/

        [Description("Gets all the grids in a Cylindrical Grid System")]
        private List<Grid> CylindricalGrids(string id, List<string> ids)
        {
            double xO = 0;
            double yO = 0;
            double rZ = 0;

            bool storyRangeDefault = true;
            string top = "";
            string bottom = "";

            double bubbleSize = 0;
            int gridColour = -1;

            int numRLines = 0;
            string[] gridLineIDR = null;
            double[] ordinateR = null;
            bool[] visibleR = null;
            string[] bubbleLocR = null;

            int numTLines = 0;
            string[] gridLineIDT = null;
            double[] ordinateT = null;
            bool[] visibleT = null;
            string[] bubbleLocT = null;

            if (m_model.GridSys.GetGridSysCylindrical(
                id, ref xO, ref yO, ref rZ,
                ref storyRangeDefault, ref top, ref bottom,
                ref bubbleSize, ref gridColour,
                ref numRLines, ref gridLineIDR, ref ordinateR, ref visibleR, ref bubbleLocR,
                ref numTLines, ref gridLineIDT, ref ordinateT, ref visibleT, ref bubbleLocT) == 1)
            {
                return new List<Grid>();
            }

            // Get bounds
            double minX = ordinateR.Min();
            double maxX = ordinateR.Max();

            // Format the names like the Lines
            List<string> names = gridLineIDR.Concat(gridLineIDT).ToList();
            
            // Create the Lines
            List<ICurve> result = ordinateR.Select(r =>
            {
                List<Point> cPoints = new List<Point>();
                foreach (double radians in ordinateT)
                {
                    Point dir = (new Point() { X = 1 }).Rotate(Point.Origin, Vector.ZAxis, radians * Math.PI / 180);
                    cPoints.Add(dir * r);
                }
                return new Polyline() { ControlPoints = cPoints };
            }
            ).ToList<ICurve>();
            result.AddRange(ordinateT.Select(radians =>
            {
                Point dir = (new Point() { X = 1 }).Rotate(Point.Origin, Vector.ZAxis, radians * Math.PI / 180);
                return new Line()
                {
                    Start = dir * minX,
                    End = dir * maxX
                };
            }
            ));

            // Place at gridsystem origin and orientation
            result = result.Select(x => Engine.Geometry.Modify.IRotate(x, Point.Origin, Vector.ZAxis, rZ * Math.PI / 180)).ToList();
            Vector origin = new Vector() { X = xO, Y = yO };
            result = result.Select(x => Engine.Geometry.Modify.ITranslate(x, origin)).ToList();

            // Remove un-requested grids if needed
            if (ids != null)
                return result.Zip(names, (x, name) => new Grid() { Curve = x, Name = name }).Where(grid => ids.Contains(grid.Name)).ToList();
            else
                return result.Zip(names, (x, name) => new Grid() { Curve = x, Name = name }).ToList();

        }

        /***************************************************/

    }
}



