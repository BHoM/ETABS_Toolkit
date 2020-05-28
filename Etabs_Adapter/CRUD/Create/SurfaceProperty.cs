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


using BH.oM.Structure.SurfaceProperties;
using BH.Engine.Structure;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Fragments;
using BH.Engine.Base;

#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /***    Create Methods                           ***/
        /***************************************************/

        private bool CreateObject(ISurfaceProperty property2d)
        {
            bool success = true;
            int retA = 0;

            string propertyName = property2d.DescriptionOrName();
            property2d.CustomData[AdapterIdName] = propertyName;

            eShellType shellType = ShellTypeToCSI(property2d);

            if (property2d.GetType() == typeof(Waffle))
            {
                Waffle waffleProperty = (Waffle)property2d;
                m_model.PropArea.SetSlab(propertyName, eSlabType.Waffle, shellType, property2d.Material.DescriptionOrName(), waffleProperty.Thickness);
                retA = m_model.PropArea.SetSlabWaffle(propertyName, waffleProperty.TotalDepthX, waffleProperty.Thickness, waffleProperty.StemWidthX, waffleProperty.StemWidthX, waffleProperty.SpacingX, waffleProperty.SpacingY);
            }
            else if (property2d.GetType() == typeof(Ribbed))
            {
                Ribbed ribbedProperty = (Ribbed)property2d;
                m_model.PropArea.SetSlab(propertyName, eSlabType.Ribbed, shellType, property2d.Material.DescriptionOrName(), ribbedProperty.Thickness);
                retA = m_model.PropArea.SetSlabRibbed(propertyName, ribbedProperty.TotalDepth, ribbedProperty.Thickness, ribbedProperty.StemWidth, ribbedProperty.StemWidth, ribbedProperty.Spacing, (int)ribbedProperty.Direction);
            }
            else if (property2d.GetType() == typeof(LoadingPanelProperty))
            {
                retA = m_model.PropArea.SetSlab(propertyName, eSlabType.Slab, shellType, property2d.Material.DescriptionOrName(), 0);
            }

            else if (property2d.GetType() == typeof(ConstantThickness))
            {
                ConstantThickness constantThickness = (ConstantThickness)property2d;
                if (constantThickness.PanelType == PanelType.Wall)
                    retA = m_model.PropArea.SetWall(propertyName, eWallPropType.Specified, shellType, property2d.Material.DescriptionOrName(), constantThickness.Thickness);
                else
                    retA = m_model.PropArea.SetSlab(propertyName, eSlabType.Slab, shellType, property2d.Material.DescriptionOrName(), constantThickness.Thickness);
            }

            SurfacePropertyModifier modifier = property2d.FindFragment<SurfacePropertyModifier>();
            if (modifier != null)
            {
                double[] modifiers = new double[] { modifier.FXX, modifier.FYY, modifier.FXY, modifier.MXX, modifier.MYY, modifier.MXY, modifier.VXZ, modifier.VYZ, modifier.Mass, modifier.Weight };
                m_model.PropArea.SetModifiers(propertyName, ref modifiers);
            }

            if (retA != 0)
                success = false;

            return success;
        }
        
        
        /***************************************************/
        /***    Helper Methods                           ***/
        /***************************************************/

        private eShellType ShellTypeToCSI(ISurfaceProperty panel)
        {
            object obj;

            if (panel.CustomData.TryGetValue("ShellType", out obj) && obj is ShellType)
            {
                switch ((ShellType)obj)
                {
                    case ShellType.ShellThin:
                        return eShellType.ShellThin;
                    case ShellType.ShellThick:
                        return eShellType.ShellThick;
                    case ShellType.Membrane:
                        return eShellType.Membrane;
                }
            }
            return eShellType.ShellThin;
        }

        /***************************************************/
    }
}

