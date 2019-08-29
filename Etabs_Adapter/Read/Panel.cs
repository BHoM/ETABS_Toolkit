/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
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
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
#if (Debug2017)
using ETABSv17;
#else
using ETABS2016;
#endif
using BH.Engine.ETABS;
using BH.oM.Geometry;
using BH.Engine.Geometry;
using BH.Engine.Reflection;
using BH.oM.Architecture.Elements;
using BH.oM.Adapters.ETABS.Elements;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        /***************************************************/

        private List<Panel> ReadPanel(List<string> ids = null)
        {
            List<Panel> panelList = new List<Panel>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                m_model.AreaObj.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            //get openings, if any
            m_model.AreaObj.GetNameList(ref nameCount, ref nameArr);
            bool isOpening = false;
            Dictionary<string, Polyline> openingDict = new Dictionary<string, Polyline>();
            foreach (string name in nameArr)
            {
                m_model.AreaObj.GetOpening(name, ref isOpening);
                if (isOpening)
                {
                    openingDict.Add(name, GetPanelPerimeter(name));
                }
            }

            foreach (string id in ids)
            {
                if (openingDict.ContainsKey(id))
                    continue;

                string propertyName = "";

                m_model.AreaObj.GetProperty(id, ref propertyName);
                ISurfaceProperty panelProperty = ReadProperty2d(new List<string>() { propertyName })[0];

                Panel panel = new Panel();
                panel.CustomData[AdapterId] = id;

                Polyline pl = GetPanelPerimeter(id);

                panel.ExternalEdges = pl.SubParts().Select(x => new Edge { Curve = x }).ToList();

                foreach (KeyValuePair<string, Polyline> kvp in openingDict)
                {
                    if (pl.IsContaining(kvp.Value.ControlPoints))
                    {
                        Opening opening = new Opening();
                        opening.Edges = kvp.Value.SubParts().Select(x => new Edge { Curve = x }).ToList();
                        panel.Openings.Add(opening);
                    }
                }

                panel.Property = panelProperty;
                string PierName = "";
                string SpandrelName = "";
                m_model.AreaObj.GetPier(id, ref PierName);
                m_model.AreaObj.GetSpandrel(id, ref SpandrelName);
                panel = panel.SetSpandrel(new Spandrel { Name = SpandrelName });
                panel = panel.SetPier(new Pier { Name = PierName });

                panelList.Add(panel);
            }

            return panelList;
        }

        /***************************************************/

        private List<ISurfaceProperty> ReadProperty2d(List<string> ids = null)
        {
            List<ISurfaceProperty> propertyList = new List<ISurfaceProperty>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                m_model.PropArea.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            foreach (string id in ids)
            {
                eSlabType slabType = eSlabType.Slab;
                eShellType shellType = eShellType.ShellThin;
                eWallPropType wallType = eWallPropType.Specified;
                string material = "";
                double thickness = 0;
                int colour = 0;
                string notes = "";
                string guid = "";
                double depth = 0;
                double stemWidthTop = 0;
                double stemWidthBottom = 0;//not used
                double ribSpacing = 0;
                double ribSpacing2nd = 0;
                int direction = 0;
                double[] modifiers = new double[] { };
                bool hasModifiers = false;

                int ret = m_model.PropArea.GetSlab(id, ref slabType, ref shellType, ref material, ref thickness, ref colour, ref notes, ref guid);
                if (ret != 0)
                    m_model.PropArea.GetWall(id, ref wallType, ref shellType, ref material, ref thickness, ref colour, ref notes, ref guid);

                if (m_model.PropArea.GetModifiers(id, ref modifiers) == 0)
                    hasModifiers = true;

                if (wallType == eWallPropType.AutoSelectList)
                {
                    string[] propList = null;
                    string currentProperty = "";

                    m_model.PropArea.GetWallAutoSelectList(id, ref propList, ref currentProperty);
                    m_model.PropArea.GetWall(currentProperty, ref wallType, ref shellType, ref material, ref thickness, ref colour, ref notes, ref guid);

                    ConstantThickness panelConstant = new ConstantThickness();
                    panelConstant.Name = currentProperty;
                    panelConstant.CustomData[AdapterId] = id;
                    panelConstant.Material = ReadMaterials(new List<string>() { material })[0];
                    panelConstant.Thickness = thickness;
                    panelConstant.PanelType = PanelType.Wall;
                    panelConstant.CustomData["ShellType"] = shellType.ToBHoM();
                    if (hasModifiers)
                        panelConstant.CustomData.Add("Modifiers", modifiers);

                    propertyList.Add(panelConstant);
                }
                else
                {
                    switch (slabType)
                    {
                        case eSlabType.Ribbed:
                            Ribbed panelRibbed = new Ribbed();

                            m_model.PropArea.GetSlabRibbed(id, ref depth, ref thickness, ref stemWidthTop, ref stemWidthBottom, ref ribSpacing, ref direction);
                            panelRibbed.Name = id;
                            panelRibbed.CustomData[AdapterId] = id;
                            panelRibbed.Material = ReadMaterials(new List<string>() { material })[0];
                            panelRibbed.Thickness = thickness;
                            panelRibbed.PanelType = PanelType.Slab;
                            panelRibbed.Direction = (PanelDirection)direction;
                            panelRibbed.Spacing = ribSpacing;
                            panelRibbed.StemWidth = stemWidthTop;
                            panelRibbed.TotalDepth = depth;
                            panelRibbed.CustomData["ShellType"] = shellType.ToBHoM();
                            if (hasModifiers)
                                panelRibbed.CustomData.Add("Modifiers", modifiers);

                            propertyList.Add(panelRibbed);
                            break;
                        case eSlabType.Waffle:
                            Waffle panelWaffle = new Waffle();

                            m_model.PropArea.GetSlabWaffle(id, ref depth, ref thickness, ref stemWidthTop, ref stemWidthBottom, ref ribSpacing, ref ribSpacing2nd);
                            panelWaffle.Name = id;
                            panelWaffle.CustomData[AdapterId] = id;
                            panelWaffle.Material = ReadMaterials(new List<string>() { material })[0];
                            panelWaffle.SpacingX = ribSpacing;
                            panelWaffle.SpacingY = ribSpacing2nd;
                            panelWaffle.StemWidthX = stemWidthTop;
                            panelWaffle.StemWidthY = stemWidthTop; //ETABS does not appear to support direction dependent stem width
                            panelWaffle.Thickness = thickness;
                            panelWaffle.TotalDepthX = depth;
                            panelWaffle.TotalDepthY = depth; // ETABS does not appear to to support direction dependent depth
                            panelWaffle.PanelType = PanelType.Slab;
                            panelWaffle.CustomData["ShellType"] = shellType.ToBHoM();
                            if (hasModifiers)
                                panelWaffle.CustomData.Add("Modifiers", modifiers);

                            propertyList.Add(panelWaffle);
                            break;
                        case eSlabType.Slab:
                        case eSlabType.Drop:
                        case eSlabType.Stiff_DO_NOT_USE:
                        default:
                            ConstantThickness panelConstant = new ConstantThickness();
                            panelConstant.CustomData[AdapterId] = id;
                            panelConstant.Name = id;
                            panelConstant.Material = ReadMaterials(new List<string>() { material })[0];
                            panelConstant.Thickness = thickness;
                            panelConstant.Name = id;
                            panelConstant.PanelType = PanelType.Slab;
                            panelConstant.CustomData["ShellType"] = shellType.ToBHoM();
                            if (hasModifiers)
                                panelConstant.CustomData.Add("Modifiers", modifiers);

                            propertyList.Add(panelConstant);
                            break;
                    }
                }
            }

            return propertyList;
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
