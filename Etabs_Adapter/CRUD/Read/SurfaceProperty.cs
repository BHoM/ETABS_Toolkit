/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.Fragments;
using BH.oM.Adapters.ETABS.Fragments;

#if Debug16 || Release16
using ETABS2016;
#elif Debug17 || Release17
using ETABSv17;
#else
using CSiAPIv1;
#endif

namespace BH.Adapter.ETABS
{
#if Debug16 || Release16
    public partial class ETABS2016Adapter : BHoMAdapter
#elif Debug17 || Release17
   public partial class ETABS17Adapter : BHoMAdapter
#elif Debug21 || Release21
   public partial class ETABS21Adapter : BHoMAdapter
#else
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /***    Read Methods                             ***/
        /***************************************************/

        private List<ISurfaceProperty> ReadSurfaceProperty(List<string> ids = null)
        {
            List<ISurfaceProperty> propertyList = new List<ISurfaceProperty>();

            Dictionary<string, IMaterialFragment> bhomMaterials = GetCachedOrReadAsDictionary<string, IMaterialFragment>();

            int nameCount = 0;
            string[] nameArr = { };
            m_model.PropArea.GetNameList(ref nameCount, ref nameArr);

            ids = FilterIds(ids, nameArr);

            foreach (string id in ids)
            {
                eSlabType slabType = eSlabType.Slab;
                eShellType shellType = eShellType.ShellThin;
                eWallPropType wallType = eWallPropType.Specified;
                string material = "";
                double thickness = 0;
                int colour = 0;
                string notes = "";
                string guid = null;
                double depth = 0;
                double stemWidthTop = 0;
                double stemWidthBottom = 0;//not used
                double ribSpacing = 0;
                double ribSpacing2nd = 0;
                int direction = 0;
                double[] modifiers = new double[] { };


                int ret = m_model.PropArea.GetSlab(id, ref slabType, ref shellType, ref material, ref thickness, ref colour, ref notes, ref guid);
                if (ret != 0)
                    m_model.PropArea.GetWall(id, ref wallType, ref shellType, ref material, ref thickness, ref colour, ref notes, ref guid);

                SurfacePropertyModifier modifier = null;
                if (m_model.PropArea.GetModifiers(id, ref modifiers) == 0 && modifiers != null && modifiers.Length == 10 && modifiers.Any(x => x != 1))
                {
                    modifier = new SurfacePropertyModifier
                    {
                        FXX = modifiers[0],
                        FYY = modifiers[1],
                        FXY = modifiers[2],
                        MXX = modifiers[3],
                        MYY = modifiers[4],
                        MXY = modifiers[5],
                        VXZ = modifiers[6],
                        VYZ = modifiers[7],
                        Mass = modifiers[8],
                        Weight = modifiers[9]
                    };
                }

                IMaterialFragment bhMaterial = null;

                try
                {
                    bhMaterial = bhomMaterials[material];
                }
                catch (Exception)
                {
                    Engine.Base.Compute.RecordNote("Could not get material from ETABS. Material for surface property " + id + " will be null");
                }

                if (wallType == eWallPropType.AutoSelectList)
                {
                    string[] propList = null;
                    string currentProperty = "";

                    m_model.PropArea.GetWallAutoSelectList(id, ref propList, ref currentProperty);
                    m_model.PropArea.GetWall(currentProperty, ref wallType, ref shellType, ref material, ref thickness, ref colour, ref notes, ref guid);

                    ConstantThickness panelConstant = new ConstantThickness();
                    panelConstant.Name = currentProperty;
                    panelConstant.Material = bhMaterial;
                    panelConstant.Thickness = thickness;
                    panelConstant.PanelType = PanelType.Wall;
                    SetShellType(panelConstant, shellType);
                    if (modifier != null)
                        panelConstant.Fragments.Add(modifier);

                    SetAdapterId(panelConstant, id);
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
                            panelRibbed.Material = bhMaterial;
                            panelRibbed.Thickness = thickness;
                            panelRibbed.PanelType = PanelType.Slab;
                            panelRibbed.Direction = (PanelDirection)direction;
                            panelRibbed.Spacing = ribSpacing;
                            panelRibbed.StemWidth = stemWidthTop;
                            panelRibbed.TotalDepth = depth;
                            SetShellType(panelRibbed, shellType);
                            if (modifier != null)
                                panelRibbed.Fragments.Add(modifier);

                            SetAdapterId(panelRibbed, id);
                            propertyList.Add(panelRibbed);
                            break;
                        case eSlabType.Waffle:
                            Waffle panelWaffle = new Waffle();

                            m_model.PropArea.GetSlabWaffle(id, ref depth, ref thickness, ref stemWidthTop, ref stemWidthBottom, ref ribSpacing, ref ribSpacing2nd);
                            panelWaffle.Name = id;
                            panelWaffle.Material = bhMaterial;
                            panelWaffle.SpacingX = ribSpacing;
                            panelWaffle.SpacingY = ribSpacing2nd;
                            panelWaffle.StemWidthX = stemWidthTop;
                            panelWaffle.StemWidthY = stemWidthTop; //ETABS does not appear to support direction dependent stem width
                            panelWaffle.Thickness = thickness;
                            panelWaffle.TotalDepthX = depth;
                            panelWaffle.TotalDepthY = depth; // ETABS does not appear to to support direction dependent depth
                            panelWaffle.PanelType = PanelType.Slab;
                            SetShellType(panelWaffle, shellType);
                            if (modifier != null)
                                panelWaffle.Fragments.Add(modifier);

                            SetAdapterId(panelWaffle, id);
                            propertyList.Add(panelWaffle);
                            break;
                        case eSlabType.Slab:
                        case eSlabType.Drop:
                        case eSlabType.Stiff_DO_NOT_USE:
                        default:
                            ConstantThickness panelConstant = new ConstantThickness();
                            panelConstant.Name = id;
                            panelConstant.Material = bhMaterial;
                            panelConstant.Thickness = thickness;
                            panelConstant.Name = id;
                            panelConstant.PanelType = PanelType.Slab;
                            SetShellType(panelConstant, shellType);
                            if (modifier != null)
                                panelConstant.Fragments.Add(modifier);

                            SetAdapterId(panelConstant, id);
                            propertyList.Add(panelConstant);
                            break;
                    }
                }
            }

            return propertyList;
        }



        /***************************************************/
        /***    Helper Methods                           ***/
        /***************************************************/

        private oM.Adapters.ETABS.ShellType? ShellTypeToBHoM(eShellType shellType)
        {
            switch (shellType)
            {
                case eShellType.ShellThin:
                    return oM.Adapters.ETABS.ShellType.ShellThin;
                case eShellType.ShellThick:
                    return oM.Adapters.ETABS.ShellType.ShellThick;
                case eShellType.Membrane:
                    return oM.Adapters.ETABS.ShellType.Membrane;
                default:
                    return null;
            }
        }

        /***************************************************/

        private void SetShellType(ISurfaceProperty property, eShellType shellType)
        {
            oM.Adapters.ETABS.ShellType? bhSHellType = ShellTypeToBHoM(shellType);

            if (bhSHellType.HasValue)
                property.Fragments.AddOrReplace(new ShellTypeFragment { ShellType = bhSHellType.Value });
        }

        /***************************************************/

    }
}






