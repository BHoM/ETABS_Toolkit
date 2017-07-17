﻿using BHoM.Materials;
using BHoM.Structural.Databases;
using BHoM.Structural.Properties;
using ETABS2016;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Etabs_Adapter.Base;

namespace Etabs_Adapter.Structural.Properties
{
    public class PropertyIO
    {
        public static SectionProperty GetBarProperty(cSapModel SapModel, string name, bool isColumn, out string material)
        {
            eFramePropType type = eFramePropType.Angle;
            SapModel.PropFrame.GetTypeOAPI(name, ref type);
            string fileName = "";
            double t3 = 0;
            double t2 = 0;
            double tf = 0;
            double tw = 0;
            double tfb = 0;
            double tb2 = 0;

            int colour = 0;
            string notes = "";
            string guid = "";
            material = "";
            ShapeType outType = ShapeType.Angle;
            eMatType matType = eMatType.Steel;
            SectionProperty property = null;
            Material bhMaterial = EtabsUtils.GetMaterial(SapModel, material);

            switch (type)
            {
                case eFramePropType.I:
                    SapModel.PropFrame.GetISection(name, ref fileName, ref material, ref t3, ref t2, ref tf, ref tw, ref tb2, ref tfb, ref colour, ref notes, ref guid);
                    bhMaterial = EtabsUtils.GetMaterial(SapModel, material);
                    if (bhMaterial != null) property = SectionProperty.CreateISection(bhMaterial.Type, t2, tb2, t3, tf, tfb, tw, 0, 0);
                    break;
                case eFramePropType.Box:
                    SapModel.PropFrame.GetTube(name, ref fileName, ref material, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    bhMaterial = EtabsUtils.GetMaterial(SapModel, material);
                    if (bhMaterial != null) property = SectionProperty.CreateBoxSection(bhMaterial.Type, t3, t2, tf, tw, 0, 0);
                    break;
                case eFramePropType.Circle:
                    SapModel.PropFrame.GetCircle(name, ref fileName, ref material, ref t3, ref colour, ref notes, ref guid);
                    bhMaterial = EtabsUtils.GetMaterial(SapModel, material);
                    if (bhMaterial != null) property = SectionProperty.CreateCircularSection(bhMaterial.Type, t3);
                    break;
                case eFramePropType.Pipe:
                    SapModel.PropFrame.GetPipe(name, ref fileName, ref material, ref t3, ref tf, ref colour, ref notes, ref guid);
                    bhMaterial = EtabsUtils.GetMaterial(SapModel, material);
                    if (bhMaterial != null) property = SectionProperty.CreateTubeSection(bhMaterial.Type, t3, tf);
                    break;
                case eFramePropType.Rectangular:
                    SapModel.PropFrame.GetRectangle(name, ref fileName, ref material, ref t3, ref t2, ref colour, ref notes, ref guid);
                    bhMaterial = EtabsUtils.GetMaterial(SapModel, material);
                    if (bhMaterial != null) property = SectionProperty.CreateRectangularSection(bhMaterial.Type, t3, t2);
                    break;
                case eFramePropType.Angle:
                    SapModel.PropFrame.GetAngle(name, ref fileName, ref material, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    bhMaterial = EtabsUtils.GetMaterial(SapModel, material);
                    if (bhMaterial != null) property = SectionProperty.CreateAngleSection(bhMaterial.Type, t3, t2, tf, tw, 0, 0);
                    break;
                case eFramePropType.Channel:
                    SapModel.PropFrame.GetChannel(name, ref fileName, ref material, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    bhMaterial = EtabsUtils.GetMaterial(SapModel, material);
                    if (bhMaterial != null) property = SectionProperty.CreateChannelSection(bhMaterial.Type, t3, t2, tf, tw, 0);
                    break;
                default:
                    SapModel.PropFrame.GetRectangle(name, ref fileName, ref material, ref t3, ref t2, ref colour, ref notes, ref guid);
                    bhMaterial = Material.Default(MaterialType.Steel);
                    if (bhMaterial != null) property = SectionProperty.CreateRectangularSection(bhMaterial.Type, Math.Max(t3,0.1), Math.Max(t2, 0.1));
                    break;

            }
            if (property != null)
            {
                property.Name = name;
                property.Material = bhMaterial;
                if (isColumn) property.Orientation = Math.PI / 2;

            }
            return property;
        }

        public static PanelProperty GetPanelProperty(cSapModel SapModel, string name, out string materialName)
        {
            int type = 0;
            SapModel.PropArea.GetTypeOAPI(name, ref type);

            double t2 = 0;

            string material = "";
            int colour = 0;
            string notes = "";
            string guid = "";
            eSlabType sType = eSlabType.Drop;
            eShellType shType = eShellType.ShellThin;
            switch (type)
            {
                case 0:
                case 1:
                    SapModel.PropArea.GetSlab(name, ref sType, ref shType, ref material, ref t2, ref colour, ref notes, ref guid);
                    materialName = material;
                    if (t2 != 0)
                    {
                        return new ConstantThickness(name, t2, PanelType.Slab);
                    }
                    else
                    {
                        eWallPropType wType = eWallPropType.AutoSelectList;
                        SapModel.PropArea.GetWall(name, ref wType, ref shType, ref material, ref t2, ref colour, ref notes, ref guid);
                        materialName = material;
                        return new ConstantThickness(name, t2, PanelType.Wall);
                    }
                default:
                    materialName = "";
                    return null;
            }
        }

        public static void CreatePanelProperty(cSapModel SapModel, PanelProperty p, Material material)
        {
            if (material == null)
            {
                material = Material.Default(MaterialType.Concrete);
            }

            EtabsUtils.CreateMaterial(SapModel, material);

            if (p.Type != PanelType.Wall)
            {
                if (p is Ribbed)
                {
                    Ribbed rT = p as Ribbed;
                    SapModel.PropArea.SetSlabRibbed(p.Name, rT.TotalDepth, rT.Thickness, rT.StemWidth, rT.StemWidth, rT.Spacing, rT.Direction == PanelDirection.X ? 1 : 0);
                }
                else if (p is Waffle)
                {
                    Waffle wT = p as Waffle;
                    SapModel.PropArea.SetSlabWaffle(p.Name, wT.TotalDepthX, wT.Thickness, wT.StemWidthX, wT.StemWidthX, wT.SpacingX, wT.SpacingY);
                }
                else
                {
                    SapModel.PropArea.SetSlab(p.Name, eSlabType.Slab, eShellType.ShellThin, material.Name, p.Thickness);

                }
            }
            else
            {
                SapModel.PropArea.SetWall(p.Name, eWallPropType.Specified, eShellType.ShellThin, material.Name, p.Thickness);
            }

            if (p.Modifiers != null)
            {
                double[] modifiers = p.Modifiers;
                SapModel.PropArea.SetModifiers(p.Name, ref modifiers);
            }
        }

        public static void CreateBarProperty(cSapModel SapModel, SectionProperty b, Material material)
        {
            string materialName = "";

            if (material != null)
            {
                EtabsUtils.CreateMaterial(SapModel, material);
                materialName = material.Name;
            }
            else
            {
                int num = 0;
                string[] names = null;
                SapModel.PropMaterial.GetNameList(ref num, ref names);
                materialName = names[0];
            }

            switch (b.Shape)
            {
                case ShapeType.ISection:
                    SapModel.PropFrame.SetISection(b.Name, materialName, b.TotalDepth, b.SectionData[(int)SteelSectionData.B1],
                        b.SectionData[(int)SteelSectionData.TF1], b.SectionData[(int)SteelSectionData.TW],
                        b.SectionData[(int)SteelSectionData.B2], b.SectionData[(int)SteelSectionData.TF2]);
                    break;
                case ShapeType.Box:
                    SapModel.PropFrame.SetTube(b.Name, materialName, b.TotalDepth, b.TotalWidth, b.SectionData[(int)SteelSectionData.TF1], b.SectionData[(int)SteelSectionData.TW]);
                    break;
                case ShapeType.Circle:
                    SapModel.PropFrame.SetCircle(b.Name, materialName, b.TotalDepth);
                    break;
                case ShapeType.Tube:
                    SapModel.PropFrame.SetPipe(b.Name, materialName, b.TotalDepth, b.SectionData[(int)SteelSectionData.TF1]);
                    break;
                case ShapeType.Rectangle:
                    SapModel.PropFrame.SetRectangle(b.Name, materialName, b.TotalDepth, b.TotalWidth);
                    break;
                case ShapeType.Angle:
                    SapModel.PropFrame.SetAngle(b.Name, materialName, b.TotalDepth, b.TotalWidth, b.SectionData[(int)SteelSectionData.TF1], b.SectionData[(int)SteelSectionData.TW]);
                    break;
                case ShapeType.Tee:
                    SapModel.PropFrame.SetTee(b.Name, materialName, b.TotalDepth, b.TotalWidth, b.SectionData[(int)SteelSectionData.TF1], b.SectionData[(int)SteelSectionData.TW]);
                    break;
            }
        }
    
        //public static SectionType GetSectionType(eMatType m, bool IsColumn)
        //{
        //    switch (m)
        //    {
        //        case eMatType.Concrete:
        //            return IsColumn ? SectionType.ConcreteColumn : SectionType.ConcreteBeam;
        //        case eMatType.Steel:
        //            return SectionType.Steel;
        //        case eMatType.NoDesign:
        //            return SectionType.Timber;
        //        case eMatType.Aluminum:
        //            return SectionType.Aluminium;
        //        default:
        //            return SectionType.Steel;
        //    }
        //}

        public static NodeConstraint GetNodeConstraint(bool[] restraint, double[] values)
        {
            if (restraint == null)
            {
                restraint = new bool[6];
            }
            if (values == null)
            {
                values = new double[6];
            }

            NodeConstraint constraint = new NodeConstraint("", restraint, values);
            constraint.Name = constraint.ToString();
            return constraint;
        }     
    }
}
