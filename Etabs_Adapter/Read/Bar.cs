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
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;
using BH.oM.Geometry;
using BH.oM.Geometry.ShapeProfiles;
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

        private List<Bar> ReadBar(List<string> ids = null)
        {
            List<Bar> barList = new List<Bar>();
            Dictionary<string, Node> bhomNodes = ReadNode().ToDictionary(x => x.CustomData[AdapterId].ToString());
            Dictionary<string, ISectionProperty> bhomSections = ReadSectionProperty().ToDictionary(x => x.CustomData[AdapterId].ToString());

            int nameCount = 0;
            string[] names = { };
            m_model.FrameObj.GetNameList(ref nameCount, ref names);

            if (ids == null)
            {
                ids = names.ToList();
            }            

            foreach (string id in ids)
            {
                try
                {
                    Bar bhBar = new Bar();
                    bhBar.CustomData.Add(AdapterId, id);
                    string startId = "";
                    string endId = "";
                    m_model.FrameObj.GetPoints(id, ref startId, ref endId);

                    bhBar.StartNode = bhomNodes[startId];
                    bhBar.EndNode = bhomNodes[endId];

                    bool[] restraintStart = new bool[6];
                    double[] springStart = new double[6];
                    bool[] restraintEnd = new bool[6];
                    double[] springEnd = new double[6];

                    m_model.FrameObj.GetReleases(id, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd);
                    bhBar.Release = GetBarRelease(restraintStart, springStart, restraintEnd, springEnd);
                    
                    string propertyName = "";
                    string sAuto = "";
                    m_model.FrameObj.GetSection(id, ref propertyName, ref sAuto);
                    if (propertyName != "None")
                    {
                        bhBar.SectionProperty = bhomSections[propertyName];
                    }

                    bool autoOffset = false;
                    double startLength = 0;
                    double endLength = 0;
                    double rz = 0;
                    m_model.FrameObj.GetEndLengthOffset(id, ref autoOffset, ref startLength, ref endLength, ref rz);
                    if (!autoOffset)
                    {
                        bhBar.Offset = new oM.Structure.Offsets.Offset();
                        bhBar.Offset.Start = startLength == 0 ? null : new Vector() { X = startLength * (-1), Y = 0, Z = 0 };
                        bhBar.Offset.End = endLength == 0 ? null : new Vector() { X = endLength, Y = 0, Z = 0 };
                    }
                    else if (rz > 0)
                    {
                        bhBar = bhBar.SetAutoLengthOffset(autoOffset, rz);
                    }

                    // OrientationAngle
                    double angle = 0;
                    bool advanced = false;
                    m_model.FrameObj.GetLocalAxes(id, ref angle, ref advanced);
                    if (!advanced)
                        bhBar.OrientationAngle = angle * Math.PI / 180;
                    else
                        BH.Engine.Reflection.Compute.RecordWarning("advanced local axis for bars are not supported");

                    barList.Add(bhBar);
                }
                catch
                {
                    BH.Engine.Reflection.Compute.RecordError("Bar " + id.ToString() + " could not be pulled");
                }
            }
            return barList;
        }

        /***************************************************/

        private List<ISectionProperty> ReadSectionProperty(List<string> ids = null)
        {
            List<ISectionProperty> propList = new List<ISectionProperty>();
            Dictionary<String, IMaterialFragment> bhomMaterials = ReadMaterial().ToDictionary(x => x.Name);

            int nameCount = 0;
            string[] names = { };
            m_model.PropFrame.GetNameList(ref nameCount, ref names);

            if (ids == null)
            {
                ids = names.ToList();
            }

            eFramePropType propertyType = eFramePropType.General;

            foreach (string id in ids)
            {
                m_model.PropFrame.GetTypeOAPI((string)id, ref propertyType);

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
                        m_model.PropFrame.GetISection(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref t2b, ref tfb, ref colour, ref notes, ref guid);
                        if (t2 == t2b)
                            dimensions = Engine.Geometry.Create.ISectionProfile(t3, t2, tw, tf, 0, 0);
                        else
                            dimensions = Engine.Geometry.Create.FabricatedISectionProfile(t3, t2, t2b, tw, tf, tfb, 0);
                        break;
                    case eFramePropType.Channel:
                        m_model.PropFrame.GetChannel(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.ChannelProfile(t3, t2, tw, tf, 0, 0);
                        break;
                    case eFramePropType.T:
                        break;
                    case eFramePropType.Angle:
                        m_model.PropFrame.GetAngle(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.AngleProfile(t3, t2, tw, tf, 0, 0);
                        break;
                    case eFramePropType.DblAngle:
                        break;
                    case eFramePropType.Box:
                        m_model.PropFrame.GetTube(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                        if (tf == tw)
                            dimensions = Engine.Geometry.Create.BoxProfile(t3, t2, tf, 0, 0);
                        else
                            dimensions = Engine.Geometry.Create.FabricatedBoxProfile(t3, t2, tw, tf, tf, 0);
                        break;
                    case eFramePropType.Pipe:
                        m_model.PropFrame.GetPipe(id, ref fileName, ref materialName, ref t3, ref tw, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.TubeProfile(t3, tw);
                        break;
                    case eFramePropType.Rectangular:
                        m_model.PropFrame.GetRectangle(id, ref fileName, ref materialName, ref t3, ref t2, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.RectangleProfile(t3, t2, 0);
                        break;
                    case eFramePropType.Auto://not member will have this assigned but it still exists in the propertyType list
                        dimensions = Engine.Geometry.Create.CircleProfile(0.2);
                        break;
                    case eFramePropType.Circle:
                        m_model.PropFrame.GetCircle(id, ref fileName, ref materialName, ref t3, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.CircleProfile(t3);
                        break;
                    case eFramePropType.General:
                        constructSelector = "explicit";
                        m_model.PropFrame.GetGeneral(id, ref fileName, ref materialName, ref t3, ref t2, ref Area, ref As2, ref As3, ref Torsion, ref I22, ref I33, ref S22, ref S33, ref Z22, ref Z33, ref R22, ref R33, ref colour, ref notes, ref guid);
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
                        m_model.PropFrame.GetConcreteL(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref twt, ref horFlip, ref verticalFlip, ref colour, ref notes, ref guid);
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
                        m_model.PropFrame.GetConcreteTee(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref twt, ref verticalFlip, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.TSectionProfile(t2, t2, tw, tf, 0, 0, verticalFlip);
                        break;
                    case eFramePropType.ConcreteBox:
                        m_model.PropFrame.GetTube(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                        if (tf == tw)
                            dimensions = Engine.Geometry.Create.BoxProfile(t3, t2, tf, 0, 0);
                        else
                            dimensions = Engine.Geometry.Create.FabricatedBoxProfile(t3, t2, tw, tf, tf, 0);
                        break;
                    case eFramePropType.ConcretePipe:
                        m_model.PropFrame.GetPipe(id, ref fileName, ref materialName, ref t3, ref tw, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.TubeProfile(t3, tw);
                        break;
                    case eFramePropType.ConcreteCross:
                        break;
                    case eFramePropType.SteelPlate:
                        m_model.PropFrame.GetPlate(id, ref fileName, ref materialName, ref t3, ref t2, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.RectangleProfile(t3, t2, 0);
                        break;
                    case eFramePropType.SteelRod:
                        m_model.PropFrame.GetRod(id, ref fileName, ref materialName, ref t3, ref colour, ref notes, ref guid);
                        dimensions = Engine.Geometry.Create.CircleProfile(t3);
                        break;
                    default:
                        break;
                }
                if (dimensions == null)
                {
                    Engine.Reflection.Compute.RecordNote(propertyType.ToString() + " properties are not implemented in ETABS adapter. An empty section has been returned.");
                    constructSelector = "explicit";
                }
                #endregion


                IMaterialFragment material;
                try
                {
                    material = bhomMaterials[materialName];
                }
                catch (Exception)
                {
                    material = bhomMaterials.FirstOrDefault().Value;
                    Engine.Reflection.Compute.RecordNote("Could not get material from ETABS. Using a default material");
                }


                switch (constructSelector)
                {
                    case "fromDimensions":
                        if (material is Steel || material is Aluminium)

                            bhSectionProperty = Engine.Structure.Create.SteelSectionFromProfile(dimensions);
                        else if (material is Concrete)
                            bhSectionProperty = Engine.Structure.Create.ConcreteSectionFromProfile(dimensions);
                        else
                        {
                            Engine.Reflection.Compute.RecordWarning("Could not create " + propertyType.ToString() + ". Nothing was returned.");
                            return null;
                        }

                        break;
                    case "explicit":
                        bhSectionProperty = new ExplicitSection()
                        {
                            Area = Area,
                            Asy = As2,
                            Asz = As3,
                            Iy = I22,
                            Iz = I33,
                            J = Torsion,
                            Rgy = R22,
                            Rgz = R33,
                            Wply = S22,//capacity - plastic (wply)
                            Wplz = S33,
                            Wely = Z22,//capacity elastic
                            Welz = Z33
                        };
                        break;
                    default:
                        break;
                }

                bhSectionProperty.Material = material;
                bhSectionProperty.Name = id;
                bhSectionProperty.CustomData[AdapterId] = id;

                propList.Add(bhSectionProperty);
            }

            return propList;
        }

        /***************************************************/

        public static BarRelease GetBarRelease(bool[] startRestraint, double[] startSpring, bool[] endRestraint, double[] endSpring)
        {
            Constraint6DOF startRelease = new Constraint6DOF();

            startRelease.TranslationX = startRestraint[0] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.TranslationY = startRestraint[1] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.TranslationZ = startRestraint[2] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.RotationX = startRestraint[3] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.RotationY = startRestraint[4] == true ? DOFType.Free : DOFType.Fixed;
            startRelease.RotationZ = startRestraint[5] == true ? DOFType.Free : DOFType.Fixed;

            startRelease.TranslationalStiffnessX = startSpring[0];
            startRelease.TranslationalStiffnessY = startSpring[1];
            startRelease.TranslationalStiffnessZ = startSpring[2];
            startRelease.RotationalStiffnessX = startSpring[3];
            startRelease.RotationalStiffnessY = startSpring[4];
            startRelease.RotationalStiffnessZ = startSpring[5];

            Constraint6DOF endRelease = new Constraint6DOF();

            endRelease.TranslationX = endRestraint[0] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.TranslationY = endRestraint[1] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.TranslationZ = endRestraint[2] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.RotationX = endRestraint[3] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.RotationY = endRestraint[4] == true ? DOFType.Free : DOFType.Fixed;
            endRelease.RotationZ = endRestraint[5] == true ? DOFType.Free : DOFType.Fixed;

            endRelease.TranslationalStiffnessX = endSpring[0];
            endRelease.TranslationalStiffnessY = endSpring[1];
            endRelease.TranslationalStiffnessZ = endSpring[2];
            endRelease.RotationalStiffnessX = endSpring[3];
            endRelease.RotationalStiffnessY = endSpring[4];
            endRelease.RotationalStiffnessZ = endSpring[5];

            BarRelease barRelease = new BarRelease() { StartRelease = startRelease, EndRelease = endRelease };

            return barRelease;
        }

    }
}
