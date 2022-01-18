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


using BH.oM.Structure.SurfaceProperties;
using BH.Engine.Structure;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Fragments;
using BH.Engine.Base;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS.Fragments;

#if Debug16 || Release16
using ETABS2016;
#elif Debug17 || Release17
using ETABSv17;
#else
using ETABSv1;
#endif

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

        private bool CreateObject(ISurfaceProperty property2d)
        {
            bool success = true;
            int retA = 0;

            string propertyName = property2d.DescriptionOrName();
            SetAdapterId(property2d, propertyName);

            string materialName = "";

            if (CheckPropertyWarning(property2d, p => p.Material))
            {
                materialName = GetAdapterId<string>(property2d.Material) ?? "";
            }

            eShellType shellType = ShellTypeToCSI(property2d);

            if (property2d.GetType() == typeof(Waffle))
            {
                Waffle waffleProperty = (Waffle)property2d;
                m_model.PropArea.SetSlab(propertyName, eSlabType.Waffle, shellType, materialName, waffleProperty.Thickness);
                retA = m_model.PropArea.SetSlabWaffle(propertyName, waffleProperty.TotalDepthX, waffleProperty.Thickness, waffleProperty.StemWidthX, waffleProperty.StemWidthX, waffleProperty.SpacingX, waffleProperty.SpacingY);
            }
            else if (property2d.GetType() == typeof(Ribbed))
            {
                Ribbed ribbedProperty = (Ribbed)property2d;
                m_model.PropArea.SetSlab(propertyName, eSlabType.Ribbed, shellType, materialName, ribbedProperty.Thickness);
                retA = m_model.PropArea.SetSlabRibbed(propertyName, ribbedProperty.TotalDepth, ribbedProperty.Thickness, ribbedProperty.StemWidth, ribbedProperty.StemWidth, ribbedProperty.Spacing, (int)ribbedProperty.Direction);
            }
            else if (property2d.GetType() == typeof(LoadingPanelProperty))
            {
                retA = m_model.PropArea.SetSlab(propertyName, eSlabType.Slab, shellType, materialName, 0);
            }
            else if (property2d.GetType() == typeof(ConstantThickness))
            {
                ConstantThickness constantThickness = (ConstantThickness)property2d;
                if (constantThickness.PanelType == PanelType.Wall)
                    retA = m_model.PropArea.SetWall(propertyName, eWallPropType.Specified, shellType, materialName, constantThickness.Thickness);
                else
                    retA = m_model.PropArea.SetSlab(propertyName, eSlabType.Slab, shellType, materialName, constantThickness.Thickness);
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

        private eShellType ShellTypeToCSI(ISurfaceProperty property)
        {
            ShellTypeFragment shelltypeFr = property.FindFragment<ShellTypeFragment>();

            if (shelltypeFr != null)
            {
                switch (shelltypeFr.ShellType)
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



