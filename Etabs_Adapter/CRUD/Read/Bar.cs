/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2021, the respective contributors. All rights reserved.
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
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.Engine.Adapters.ETABS;
using BH.oM.Geometry;
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
            Dictionary<string, Node> bhomNodes = ReadNode().ToDictionary(x => GetAdapterId<string>(x));
            Dictionary<string, ISectionProperty> bhomSections = ReadSectionProperty().ToDictionary(x => GetAdapterId<string>(x));

            int nameCount = 0;
            string[] names = { };
            m_model.FrameObj.GetNameList(ref nameCount, ref names);

            ids = FilterIds(ids, names);

            foreach (string id in ids)
            {
                ETABSId etabsIdFragment = new ETABSId();
                etabsIdFragment.Id = id;

                try
                {
                    Bar bhBar = new Bar();
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
                    string guid = null;

                    if (m_model.FrameObj.GetLabelFromName(id, ref label, ref story) == 0)
                    {
                        etabsIdFragment.Label = label;
                        etabsIdFragment.Story = story;
                    }

                    if (m_model.AreaObj.GetGUID(id, ref guid) == 0)
                        etabsIdFragment.PersistentId = guid;

                    bhBar.SetAdapterId(etabsIdFragment);
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

        public static BarRelease GetBarRelease(bool[] startRelease, double[] startSpring, bool[] endRelease, double[] endSpring)
        {
            Constraint6DOF bhStartRelease = new Constraint6DOF();

            bhStartRelease.TranslationX = GetDofType(startRelease, startSpring, 0);
            bhStartRelease.TranslationY = GetDofType(startRelease, startSpring, 2);
            bhStartRelease.TranslationZ = GetDofType(startRelease, startSpring, 1);
            bhStartRelease.RotationX = GetDofType(startRelease, startSpring, 3);
            bhStartRelease.RotationY = GetDofType(startRelease, startSpring, 5);
            bhStartRelease.RotationZ = GetDofType(startRelease, startSpring, 4);

            bhStartRelease.TranslationalStiffnessX = startSpring[0];
            bhStartRelease.TranslationalStiffnessY = startSpring[2];
            bhStartRelease.TranslationalStiffnessZ = startSpring[1];
            bhStartRelease.RotationalStiffnessX = startSpring[3];
            bhStartRelease.RotationalStiffnessY = startSpring[5];
            bhStartRelease.RotationalStiffnessZ = startSpring[4];

            Constraint6DOF bhEndRelease = new Constraint6DOF();

            bhEndRelease.TranslationX = GetDofType(endRelease, endSpring, 0);
            bhEndRelease.TranslationY = GetDofType(endRelease, endSpring, 2);
            bhEndRelease.TranslationZ = GetDofType(endRelease, endSpring, 1);
            bhEndRelease.RotationX = GetDofType(endRelease, endSpring, 3);
            bhEndRelease.RotationY = GetDofType(endRelease, endSpring, 5);
            bhEndRelease.RotationZ = GetDofType(endRelease, endSpring, 4);

            bhEndRelease.TranslationalStiffnessX = endSpring[0];
            bhEndRelease.TranslationalStiffnessY = endSpring[2];
            bhEndRelease.TranslationalStiffnessZ = endSpring[1];
            bhEndRelease.RotationalStiffnessX = endSpring[3];
            bhEndRelease.RotationalStiffnessY = endSpring[5];
            bhEndRelease.RotationalStiffnessZ = endSpring[4];

            BarRelease barRelease = new BarRelease() { StartRelease = bhStartRelease, EndRelease = bhEndRelease };

            return barRelease;
        }

        /***************************************************/

        private static DOFType GetDofType(bool[] isReleased, double[] springValue, int i)
        {
            if (isReleased[i])
            {
                if (springValue[i] != 0)
                    return DOFType.Spring;
                else
                    return DOFType.Free;
            }
            else
                return DOFType.Fixed;
        }

        /***************************************************/

    }
}


