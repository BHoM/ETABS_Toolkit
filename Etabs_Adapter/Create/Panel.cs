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

using System.Collections.Generic;
using System.Linq;
using BH.oM.Architecture.Elements;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Loads;
using BH.oM.Structure.Offsets;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;
using BH.oM.Adapters.ETABS.Elements;
#if Debug2017
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        /***************************************************/

        private bool CreateObject(Panel bhPanel)
        {
            bool success = true;
            int retA = 0;

            double mergeTol = 1e-3; //Merging panel points to the mm, same behaviour as the default node comparer

            string name = "";
            string propertyName = bhPanel.Property.Name;
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

            bhPanel.CustomData[AdapterId] = name;

            if (retA != 0)
                return false;

            if (bhPanel.Openings != null)
            {
                for (int i = 0; i < bhPanel.Openings.Count; i++)
                {
                    boundaryPoints = bhPanel.Openings[i].ControlPoints().CullDuplicates(mergeTol);

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
                }
            }


            Diaphragm diaphragm = bhPanel.Diaphragm();

            if (diaphragm != null)
            {
                m_model.AreaObj.SetDiaphragm(name, diaphragm.Name);
            }

            return success;
        }

        /***************************************************/

        private bool CreateObject(ISurfaceProperty property2d)
        {
            bool success = true;
            int retA = 0;

            string propertyName = "None";
            if (property2d.Name != "")
            {
                property2d.CustomData[AdapterId] = propertyName = property2d.Name;
            }
            else
            {
                BH.Engine.Reflection.Compute.RecordNote("Surface properties with no name will be assigned 'None'.");
                property2d.CustomData[AdapterId] = "None";
                return true;
            }

            eShellType shellType = property2d.EtabsShellType();

            if (property2d.GetType() == typeof(Waffle))
            {
                Waffle waffleProperty = (Waffle)property2d;
                m_model.PropArea.SetSlab(propertyName, eSlabType.Waffle, shellType, property2d.Material.Name, waffleProperty.Thickness);
                retA = m_model.PropArea.SetSlabWaffle(propertyName, waffleProperty.TotalDepthX, waffleProperty.Thickness, waffleProperty.StemWidthX, waffleProperty.StemWidthX, waffleProperty.SpacingX, waffleProperty.SpacingY);
            }
            else if (property2d.GetType() == typeof(Ribbed))
            {
                Ribbed ribbedProperty = (Ribbed)property2d;
                m_model.PropArea.SetSlab(propertyName, eSlabType.Ribbed, shellType, property2d.Material.Name, ribbedProperty.Thickness);
                retA = m_model.PropArea.SetSlabRibbed(propertyName, ribbedProperty.TotalDepth, ribbedProperty.Thickness, ribbedProperty.StemWidth, ribbedProperty.StemWidth, ribbedProperty.Spacing, (int)ribbedProperty.Direction);
            }
            else if (property2d.GetType() == typeof(LoadingPanelProperty))
            {
                retA = m_model.PropArea.SetSlab(propertyName, eSlabType.Slab, shellType, property2d.Material.Name, 0);
            }

            else if (property2d.GetType() == typeof(ConstantThickness))
            {
                ConstantThickness constantThickness = (ConstantThickness)property2d;
                if (constantThickness.PanelType == PanelType.Wall)
                    retA = m_model.PropArea.SetWall(propertyName, eWallPropType.Specified, shellType, property2d.Material.Name, constantThickness.Thickness);
                else
                    retA = m_model.PropArea.SetSlab(propertyName, eSlabType.Slab, shellType, property2d.Material.Name, constantThickness.Thickness);
            }


            if (property2d.HasModifiers())
            {
                double[] modifier = property2d.Modifiers();//(double[])property2d.CustomData["Modifiers"];
                m_model.PropArea.SetModifiers(propertyName, ref modifier);
            }

            if (retA != 0)
                success = false;

            return success;
        }

        /***************************************************/

        private bool CreateObject(Diaphragm diaphragm)
        {
            bool sucess = true;
            sucess &= m_model.Diaphragm.SetDiaphragm(diaphragm.Name, diaphragm.Rigidity == oM.Adapters.ETABS.DiaphragmType.SemiRigidDiaphragm) == 0;

            return sucess;
        }

        /***************************************************/
    }
}
