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
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Geometry;
using BH.oM.Spatial.ShapeProfiles;
using BH.Engine.Structure;
using BH.oM.Structure.Fragments;
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
#else
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/

        private List<ISectionProperty> ReadSectionProperty(List<string> ids = null)
        {
            List<ISectionProperty> propList = new List<ISectionProperty>();
            Dictionary<string, IMaterialFragment> bhomMaterials = GetCachedOrReadAsDictionary<string, IMaterialFragment>();

            int nameCount = 0;
            string[] names = { };
            m_model.PropFrame.GetNameList(ref nameCount, ref names);

            ids = FilterIds(ids, names);

            eFramePropType propertyType = eFramePropType.General;

            List<string> backlog = new List<string>();
            do
            {
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
                    string guid = null;

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
                                dimensions = Engine.Spatial.Create.ISectionProfile(t3, t2, tw, tf, 0, 0);
                            else
                                dimensions = Engine.Spatial.Create.FabricatedISectionProfile(t3, t2, t2b, tw, tf, tfb, 0);
                            break;
                        case eFramePropType.Channel:
                            m_model.PropFrame.GetChannel(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                            dimensions = Engine.Spatial.Create.ChannelProfile(t3, t2, tw, tf, 0, 0);
                            break;
                        case eFramePropType.T:
                            break;
                        case eFramePropType.Angle:
                            m_model.PropFrame.GetAngle(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                            dimensions = Engine.Spatial.Create.AngleProfile(t3, t2, tw, tf, 0, 0);
                            break;
                        case eFramePropType.DblAngle:
                            break;
                        case eFramePropType.Box:
                            m_model.PropFrame.GetTube(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                            if (tf == tw)
                                dimensions = Engine.Spatial.Create.BoxProfile(t3, t2, tf, 0, 0);
                            else
                                dimensions = Engine.Spatial.Create.FabricatedBoxProfile(t3, t2, tw, tf, tf, 0);
                            break;
                        case eFramePropType.Pipe:
                            m_model.PropFrame.GetPipe(id, ref fileName, ref materialName, ref t3, ref tw, ref colour, ref notes, ref guid);
                            dimensions = Engine.Spatial.Create.TubeProfile(t3, tw);
                            break;
                        case eFramePropType.Rectangular:
                            m_model.PropFrame.GetRectangle(id, ref fileName, ref materialName, ref t3, ref t2, ref colour, ref notes, ref guid);
                            dimensions = Engine.Spatial.Create.RectangleProfile(t3, t2, 0);
                            break;
                        case eFramePropType.Auto://not member will have this assigned but it still exists in the propertyType list
                            dimensions = Engine.Spatial.Create.CircleProfile(0.2);
                            break;
                        case eFramePropType.Circle:
                            m_model.PropFrame.GetCircle(id, ref fileName, ref materialName, ref t3, ref colour, ref notes, ref guid);
                            dimensions = Engine.Spatial.Create.CircleProfile(t3);
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

                            if (!backlog.Contains(id))
                            {
                                // the sub sections need to exist to get their profiles
                                backlog.Add(id);
                                continue;
                            }
                            backlog.Remove(id);

                            int numberItems = 0;
                            string[] startSec = null;
                            string[] endSec = null;
                            double[] myLength = null;
                            int[] myType = null;
                            int[] ei33 = null;
                            int[] ei22 = null;

                            m_model.PropFrame.GetNonPrismatic(id, ref numberItems, ref startSec, ref endSec, ref myLength, ref myType, ref ei33, ref ei22, ref colour, ref notes, ref guid);

                            // Check type
                            if (myType.Any(x => x != 1))
                            {
                                Engine.Base.Compute.RecordWarning("BhoM can only pull Non-prismatic sections with relative length values.");
                                break;
                            }

                            // convert length values to relative positions between 0 and 1
                            List<double> positions = new List<double>() { 0 };
                            for (int i = 0; i < myLength.Length; i++)
                            {
                                positions.Add(System.Convert.ToDouble(myLength[i]) + positions[i]);
                            }
                            double totLength = myLength.Sum();
                            positions = positions.Select(x => x / System.Convert.ToDouble(totLength)).ToList();

                            // Getting a Profile from a name in the propList
                            Func<string, IProfile> getProfile = sectionName =>
                            {
                                ISectionProperty sec = propList.First(x => x.DescriptionOrName() == sectionName);
                                if (sec is IGeometricalSection)
                                    return (sec as IGeometricalSection).SectionProfile;
                                else
                                    return null;
                            };

                            // convert start/end section for each segment to a list of profiles (where each profile is both start and end for two segments)
                            List<IProfile> profiles = new List<IProfile>();
                            profiles.Add(getProfile(startSec.First()));
                            endSec.ToList().ForEach(x => profiles.Add(getProfile(x)));

                            // check that start/end section are equal
                            for (int i = numberItems - 1; i >= 1; i--)
                            {
                                if (startSec[i] != endSec[i - 1])
                                {
                                    profiles.Insert(i + 1, getProfile(startSec[i]));
                                    positions.Insert(i + 1, positions[i] + System.Convert.ToDouble(Tolerance.Distance));
                                }
                            }

                            // Find the material of all elements and create a singel one (or have warning)
                            foreach (string subSectionName in startSec.Concat(endSec))
                            {
                                ISectionProperty sec = propList.First(x => x.DescriptionOrName() == subSectionName);
                                if (materialName == "")
                                    materialName = sec.Material.DescriptionOrName();
                                else if (materialName != sec.Material.DescriptionOrName())
                                {
                                    materialName = "";
                                    Engine.Base.Compute.RecordWarning("All sub-sections must have the same material.");
                                    break;
                                }
                            }

                            //Etabs can only accept sections that vary linearly along it's length
                            List<int> interpolationOrder = Enumerable.Repeat(1, positions.Count - 1).ToList();

                            if (profiles.Any(x => x == null))
                            {
                                Engine.Base.Compute.RecordNote("Tapered profiles require the sub-profiles to be geometrically defined, they can not be any kind of explicit section.");
                            }
                            else
                            {
                                dimensions = Engine.Spatial.Create.TaperedProfile(positions, profiles, interpolationOrder);
                            }
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
                            dimensions = Engine.Spatial.Create.AngleProfile(t3, t2, tw, tf, 0, 0, horFlip, verticalFlip);
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
                            dimensions = Engine.Spatial.Create.TSectionProfile(t3, t2, tw, tf, 0, 0, verticalFlip);
                            break;
                        case eFramePropType.ConcreteBox:
                            m_model.PropFrame.GetTube(id, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                            if (tf == tw)
                                dimensions = Engine.Spatial.Create.BoxProfile(t3, t2, tf, 0, 0);
                            else
                                dimensions = Engine.Spatial.Create.FabricatedBoxProfile(t3, t2, tw, tf, tf, 0);
                            break;
                        case eFramePropType.ConcretePipe:
                            m_model.PropFrame.GetPipe(id, ref fileName, ref materialName, ref t3, ref tw, ref colour, ref notes, ref guid);
                            dimensions = Engine.Spatial.Create.TubeProfile(t3, tw);
                            break;
                        case eFramePropType.ConcreteCross:
                            break;
                        case eFramePropType.SteelPlate:
                            m_model.PropFrame.GetPlate(id, ref fileName, ref materialName, ref t3, ref t2, ref colour, ref notes, ref guid);
                            dimensions = Engine.Spatial.Create.RectangleProfile(t3, t2, 0);
                            break;
                        case eFramePropType.SteelRod:
                            m_model.PropFrame.GetRod(id, ref fileName, ref materialName, ref t3, ref colour, ref notes, ref guid);
                            dimensions = Engine.Spatial.Create.CircleProfile(t3);
                            break;
                        default:
                            break;
                    }
                    if (dimensions == null)
                    {
                        Engine.Base.Compute.RecordNote(propertyType.ToString() + " properties are not implemented in ETABS adapter. An empty section has been returned.");
                        constructSelector = "explicit";
                    }
                    #endregion


                    IMaterialFragment material;

                    if (!bhomMaterials.TryGetValue(materialName, out material))
                    {
                        material = new GenericIsotropicMaterial() { Name = materialName };
                        Engine.Base.Compute.RecordNote("Could not get material from ETABS. GenericIsotropic material with 0 values have been extracted in its place.");
                    }

                    switch (constructSelector)
                    {
                        case "fromDimensions":
                            bhSectionProperty = Engine.Structure.Create.SectionPropertyFromProfile(dimensions, material, id);
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
                            bhSectionProperty.Material = material;
                            bhSectionProperty.Name = id;

                            break;
                        default:
                            continue;
                    }
                    SetAdapterId(bhSectionProperty, id);

                    double[] modifiers = null;
                    if (m_model.PropFrame.GetModifiers(id, ref modifiers) == 0 && modifiers != null && modifiers.Length > 6 && modifiers.Any(x => x != 1))
                    {
                        SectionModifier modifier = new SectionModifier
                        {
                            Area = modifiers[0],
                            Asz = modifiers[1],
                            Asy = modifiers[2],
                            J = modifiers[3],
                            Iz = modifiers[4],
                            Iy = modifiers[5]
                        };
                        bhSectionProperty.Fragments.Add(modifier);
                    }

                    propList.Add(bhSectionProperty);
                }
                ids = new List<string>(backlog);
            } while (backlog.Count > 0);

            return propList;
        }

        /***************************************************/

    }
}






