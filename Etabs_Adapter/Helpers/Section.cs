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

using BH.oM.Structure.SectionProperties;
using BH.oM.Geometry.ShapeProfiles;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.Structure;
using CE = BH.Engine.Common;
using ETABS2016;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Physical.Materials;

namespace BH.Adapter.ETABS
{
    public static partial class Helper
    {
        public const string AdapterId = "ETABS_id";

        public static ISectionProperty GetSectionProperty(cSapModel model, string propertyName, eFramePropType propertyType)
        {
            //if (modelData.sectionDict.ContainsKey(propertyName))
            //    return modelData.sectionDict[propertyName];

            ISectionProperty bhSectionProperty = null;
            IProfile dimensions = null;
            string materialName = "";
            
            string fileName = "";
            double t3 = 0;
            double t2 = 0;
            double tf = 0;
            double tw = 0;
            double twt = 0;
            double tfb = 0;
            double t2b = 0;
            int colour = 0;
            string notes = "";
            string guid = "";

            bool verticalFlip = false;
            bool horFlip = false;

            double Area, As2, As3, Torsion, I22, I33, S22, S33, Z22, Z33, R22, R33;
            Area = As2 = As3 = Torsion = I22 = I33 = S22 = S33 = Z22 = Z33 = R22 = R33 = 0;

            string constructSelector = "fromDimensions";


            #region long switch on section property type
            switch (propertyType)
            {
                case eFramePropType.I:
                    model.PropFrame.GetISection(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref t2b, ref tfb, ref colour, ref notes, ref guid);
                    if (t2==t2b)
                        dimensions = Engine.Geometry.Create.ISectionProfile(t3, t2, tw, tf, 0, 0);
                    else
                        dimensions = Engine.Geometry.Create.FabricatedISectionProfile(t3, t2, t2b, tw, tf, tfb, 0);
                    break;
                case eFramePropType.Channel:
                    model.PropFrame.GetChannel(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    dimensions = Engine.Geometry.Create.ChannelProfile(t3, t2, tw, tf, 0, 0);
                    break;
                case eFramePropType.T:
                    break;
                case eFramePropType.Angle:
                    model.PropFrame.GetAngle(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    dimensions = Engine.Geometry.Create.AngleProfile(t3, t2, tw, tf, 0, 0);
                    break;
                case eFramePropType.DblAngle:
                    break;
                case eFramePropType.Box:
                    model.PropFrame.GetTube(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    if (tf == tw)
                        dimensions = Engine.Geometry.Create.BoxProfile(t3, t2, tf, 0, 0);
                    else
                        dimensions = Engine.Geometry.Create.FabricatedBoxProfile(t3, t2, tw, tf, tf, 0);
                    break;
                case eFramePropType.Pipe:
                    model.PropFrame.GetPipe(propertyName, ref fileName, ref materialName, ref t3, ref tw, ref colour, ref notes, ref guid);
                    dimensions = Engine.Geometry.Create.TubeProfile(t3, tw);
                    break;
                case eFramePropType.Rectangular:
                    model.PropFrame.GetRectangle(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref colour, ref notes, ref guid);
                    dimensions = Engine.Geometry.Create.RectangleProfile(t3, t2, 0);
                    break;
                case eFramePropType.Auto://not member will have this assigned but it still exists in the propertyType list
                    dimensions = Engine.Geometry.Create.CircleProfile(0.2);
                    break;
                case eFramePropType.Circle:
                    model.PropFrame.GetCircle(propertyName, ref fileName, ref materialName, ref t3, ref colour, ref notes, ref guid);
                    dimensions = Engine.Geometry.Create.CircleProfile(t3);
                    break;
                case eFramePropType.General:
                    constructSelector = "explicit";
                    model.PropFrame.GetGeneral(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref Area, ref As2, ref As3, ref Torsion, ref I22, ref I33, ref S22, ref S33, ref Z22, ref Z33, ref R22, ref R33, ref colour, ref notes, ref guid);
                    break;
                case eFramePropType.DbChannel:
                    break;
                case eFramePropType.SD:
                    break;
                case eFramePropType.Variable:
                    break;
                case eFramePropType.Joist:
                    break;
                case eFramePropType.Bridge:
                    break;
                case eFramePropType.Cold_C:
                    break;
                case eFramePropType.Cold_2C:
                    break;
                case eFramePropType.Cold_Z:
                    break;
                case eFramePropType.Cold_L:
                    break;
                case eFramePropType.Cold_2L:
                    break;
                case eFramePropType.Cold_Hat:
                    break;
                case eFramePropType.BuiltupICoverplate:
                    break;
                case eFramePropType.PCCGirderI:
                    break;
                case eFramePropType.PCCGirderU:
                    break;
                case eFramePropType.BuiltupIHybrid:
                    break;
                case eFramePropType.BuiltupUHybrid:
                    break;
                case eFramePropType.Concrete_L:
                    model.PropFrame.GetConcreteL(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref twt, ref horFlip, ref verticalFlip, ref colour, ref notes, ref guid);
                    dimensions = Engine.Geometry.Create.AngleProfile(t3, t2, tw, tf, 0, 0, horFlip, verticalFlip);
                    break;
                case eFramePropType.FilledTube:
                    break;
                case eFramePropType.FilledPipe:
                    break;
                case eFramePropType.EncasedRectangle:
                    break;
                case eFramePropType.EncasedCircle:
                    break;
                case eFramePropType.BucklingRestrainedBrace:
                    break;
                case eFramePropType.CoreBrace_BRB:
                    break;
                case eFramePropType.ConcreteTee:
                    model.PropFrame.GetConcreteTee(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref twt, ref verticalFlip, ref colour, ref notes, ref guid);
                    dimensions = Engine.Geometry.Create.TSectionProfile(t2, t2, tw, tf, 0, 0, verticalFlip);
                    break;
                case eFramePropType.ConcreteBox:
                    break;
                case eFramePropType.ConcretePipe:
                    break;
                case eFramePropType.ConcreteCross:
                    break;
                case eFramePropType.SteelPlate:
                    break;
                case eFramePropType.SteelRod:
                    break;
                default:
                    throw new NotImplementedException("Section convertion for the type: " + propertyType.ToString() + " is not implemented in ETABS adapter");
            }
            if(dimensions==null)
                throw new NotImplementedException("Section convertion for the type: " + propertyType.ToString() + " is not implemented in ETABS adapter");
            #endregion


            oM.Physical.Materials.Material material;
            if (materialName == "")
                material = Engine.Structure.Create.SteelMaterial("Steel");
            else
                material = GetMaterial(model, materialName);


            switch (constructSelector)
            {
                case "fromDimensions":
                    switch (material.MaterialType())
                    {
                        case MaterialType.Aluminium:
                        case MaterialType.Steel:
                            bhSectionProperty = Create.SteelSectionFromProfile(dimensions);
                            break;
                        case MaterialType.Concrete:
                            bhSectionProperty = Create.ConcreteSectionFromProfile(dimensions);
                            break;
                        case MaterialType.Timber:
                        case MaterialType.Rebar:
                        case MaterialType.Tendon:
                        case MaterialType.Glass:
                        case MaterialType.Cable:
                        default:
                            throw new NotImplementedException("no material type for " + material.MaterialType().ToString() + " implemented");
                    }
                    break;
                case "explicit":
                    ExplicitSection eSection = new ExplicitSection();
                    eSection.Area = Area;
                    eSection.Asy = As2;
                    eSection.Asz = As3;
                    //eSection.CentreY = ;
                    //eSection.CentreZ = ;
                    //eSection.Iw = 0;//warping
                    eSection.Iy = I22;
                    eSection.Iz = I33;
                    eSection.J = Torsion;
                    eSection.Rgy = R22;
                    eSection.Rgz = R33;
                    eSection.Wply = S22;//capacity - plastic (wply)
                    eSection.Wplz = S33;
                    //eSection.Vpy = 0;
                    //eSection.Vpz = 0;
                    //eSection.Vy = 0;
                    //eSection.Vz = 0;
                    eSection.Wely = Z22;//capacity elastic
                    eSection.Welz = Z33;
                    break;
                default:
                    break;
            }

            bhSectionProperty.Material = material;
            bhSectionProperty.Name = propertyName;
            bhSectionProperty.CustomData.Add(AdapterId, propertyName);
            //modelData.sectionDict.Add(propertyName, bhSectionProperty);

            return bhSectionProperty;
        }

        public static void SetSectionProperty(cSapModel model, ISectionProperty bhSection)
        {
            //without modelData and the adapter there is no reason to devide this into SetSpecificSection and SetSectionProperty... right?
            SetSpecificSection(bhSection as dynamic, model);

            double[] modifiers = bhSection.Modifiers();

            if (modifiers != null)
            {
                double[] etabsMods = new double[8];

                etabsMods[0] = modifiers[0];    //Area
                etabsMods[1] = modifiers[4];    //Minor axis shear
                etabsMods[2] = modifiers[5];    //Major axis shear
                etabsMods[3] = modifiers[3];    //Torsion
                etabsMods[4] = modifiers[1];    //Major bending
                etabsMods[5] = modifiers[2];    //Minor bending
                etabsMods[6] = 1;               //Mass, not currently implemented
                etabsMods[7] = 1;               //Weight, not currently implemented

                model.PropFrame.SetModifiers(bhSection.Name, ref etabsMods);
            }
            //string materialName = "";

            //if (modelData.sectionDict.ContainsKey(bhSection.Name))
            //{
            //    // nothing ?
            //}
            //else
            //{
            //    if (bhSection.Material == null)
            //    {
            //        //assign some default and/or throw error? TODO
            //    }
            //    else
            //    {
            //        SetMaterial(modelData, bhSection.Material);
            //    }

            //    SetSpecificSection(bhSection as dynamic, modelData.model);
            //    modelData.sectionDict.Add(bhSection.Name, bhSection);
            //}

        }

        private static void SetSpecificSection(SteelSection section, cSapModel model)
        {
            SetSectionDimensions(section.SectionProfile, section.Name, section.Material, model);
        }

        private static void SetSpecificSection(ConcreteSection section, cSapModel model)
        {
            SetSectionDimensions(section.SectionProfile, section.Name, section.Material, model);
        }

        private static void SetSpecificSection(CableSection section, cSapModel model)
        {
            //no ISectionDimentions
            throw new NotImplementedException();
        }

        private static void SetSpecificSection(CompositeSection section, cSapModel model)
        {
            //contains SteelSection and ConcreteScetion
            throw new NotImplementedException();
        }

        private static void SetSpecificSection(ExplicitSection section, cSapModel model)
        {
            model.PropFrame.SetGeneral(section.Name, section.Material.Name, section.CentreZ * 2, section.CentreY * 2, section.Area, section.Asy, section.Asz, section.J, section.Iy, section.Iz, section.Wply, section.Wplz, section.Wely, section.Wely, section.Rgy, section.Rgz);
        }

        #region section dimensions

        private static void SetSectionDimensions(IProfile sectionProfile, string sectionName, Material material, cSapModel model)
        {
            SetSpecificDimensions(sectionProfile as dynamic, sectionName, material, model);
        }

        private static void SetSpecificDimensions(TubeProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            model.PropFrame.SetPipe(sectionName, material.Name, dimensions.Diameter, dimensions.Thickness);
        }

        private static void SetSpecificDimensions(BoxProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            model.PropFrame.SetTube(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.Thickness, dimensions.Thickness);
        }

        private static void SetSpecificDimensions(FabricatedBoxProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            if (dimensions.TopFlangeThickness != dimensions.BotFlangeThickness)
                throw new NotImplementedException("different thickness of top and bottom flange is not supported in ETABS");
            model.PropFrame.SetTube(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.TopFlangeThickness, dimensions.WebThickness);
        }

        private static void SetSpecificDimensions(ISectionProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            model.PropFrame.SetISection(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.Width, dimensions.FlangeThickness);
        }

        private static void SetSpecificDimensions(FabricatedISectionProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            model.PropFrame.SetISection(sectionName, material.Name, dimensions.Height, dimensions.TopFlangeWidth, dimensions.TopFlangeThickness, dimensions.WebThickness, dimensions.BotFlangeWidth, dimensions.BotFlangeThickness);
        }

        private static void SetSpecificDimensions(ChannelProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            model.PropFrame.SetChannel(sectionName, material.Name, dimensions.Height, dimensions.FlangeWidth, dimensions.FlangeThickness, dimensions.WebThickness);
            if (dimensions.MirrorAboutLocalZ)
                RecordFlippingError(sectionName);
        }

        private static void SetSpecificDimensions(AngleProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            switch (material.MaterialType())
            {
                case MaterialType.Aluminium:
                case MaterialType.Steel:
                    model.PropFrame.SetSteelAngle(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.RootRadius, dimensions.MirrorAboutLocalZ, dimensions.MirrorAboutLocalY);
                    break;
                case MaterialType.Concrete:
                    model.PropFrame.SetConcreteL(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.WebThickness, dimensions.MirrorAboutLocalZ, dimensions.MirrorAboutLocalY);
                    break;
                case MaterialType.Timber:
                case MaterialType.Rebar:
                case MaterialType.Tendon:
                case MaterialType.Glass:
                case MaterialType.Cable:
                default:
                    model.PropFrame.SetAngle(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness);
                    if (dimensions.MirrorAboutLocalY || dimensions.MirrorAboutLocalZ)
                        RecordFlippingError(sectionName);
                    break;
            }
            
        }

        private static void SetSpecificDimensions(TSectionProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            switch (material.MaterialType())
            {
                case MaterialType.Aluminium:
                case MaterialType.Steel:
                    model.PropFrame.SetSteelTee(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.RootRadius, dimensions.MirrorAboutLocalY);
                    break;
                case MaterialType.Concrete:
                    model.PropFrame.SetConcreteTee(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness, dimensions.WebThickness, dimensions.MirrorAboutLocalY);
                    break;
                case MaterialType.Timber:
                case MaterialType.Rebar:
                case MaterialType.Tendon:
                case MaterialType.Glass:
                case MaterialType.Cable:
                default:
                    model.PropFrame.SetTee(sectionName, material.Name, dimensions.Height, dimensions.Width, dimensions.FlangeThickness, dimensions.WebThickness);
                    if (dimensions.MirrorAboutLocalY)
                        RecordFlippingError(sectionName);
                    break;
            }
           
        }

        private static void SetSpecificDimensions(ZSectionProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            throw new NotImplementedException("Zed-Section? Never heard of it!");
        }

        private static void SetSpecificDimensions(RectangleProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            model.PropFrame.SetRectangle(sectionName, material.Name, dimensions.Height, dimensions.Width);
        }

        private static void SetSpecificDimensions(CircleProfile dimensions, string sectionName, Material material, cSapModel model)
        {
            model.PropFrame.SetCircle(sectionName, material.Name, dimensions.Diameter);
        }


        private static void RecordFlippingError(string sectionName)
        {
            BH.Engine.Reflection.Compute.RecordWarning("Section with name " + sectionName + "has a flipping boolean. This is not currently supported in the Etabs_Toolkit. The section will be set to etabs unflipped");
        }

        #endregion

    }
}
