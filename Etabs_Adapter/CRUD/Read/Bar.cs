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
using BH.oM.Adapters.ETABS.Fragments;
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

        private List<Bar> ReadBar(List<string> ids = null)
        {
            List<Bar> barList = new List<Bar>();
            Dictionary<string, Node> bhomNodes = ReadNode().ToDictionary(x => x.CustomData[AdapterIdName].ToString());
            Dictionary<string, ISectionProperty> bhomSections = ReadSectionProperty().ToDictionary(x => x.CustomData[AdapterIdName].ToString());

            int nameCount = 0;
            string[] names = { };
            m_model.FrameObj.GetNameList(ref nameCount, ref names);

            ids = FilterIds(ids, names);

            foreach (string id in ids)
            {
                try
                {
                    Bar bhBar = new Bar();
                    bhBar.CustomData.Add(AdapterIdName, id);
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

                    //Label and story
                    string label = "";
                    string story = "";
                    if (m_model.FrameObj.GetLabelFromName(id, ref label, ref story) == 0)
                    {
                        EtabsLabel eLabel = new EtabsLabel { Label = label, Story = story };
                        bhBar.Fragments.Add(eLabel);
                    }

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

        /***************************************************/

    }
}

