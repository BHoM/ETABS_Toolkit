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

using System.Collections.Generic;
using System.Linq;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Offsets;
using BH.Engine.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using System.ComponentModel;
using System;
using BH.Engine.Structure;
using BH.oM.Geometry;
using BH.oM.Structure.Constraints;
using BH.Engine.Base;


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

        private bool CreateObject(Bar bhBar)
        {
            int ret = 0;

            if (!CheckPropertyError(bhBar, b => b.Start, true) || !CheckPropertyError(bhBar, b => b.End, true) ||
                !CheckPropertyError(bhBar, b => b.Start.Position, true) || !CheckPropertyError(bhBar, b => b.End.Position, true))
            {
                return false;
            }

            string name = "";

#if Debug16 || Release16

            // Evaluate if the bar is alinged as Etabs wants it
            if (bhBar.CheckFlipBar())
            {
                FlipEndPoints(bhBar);      //CloneBeforePush means this is fine
                FlipInsertionPoint(bhBar); //ETABS specific operation
                Engine.Base.Compute.RecordNote("Some bars has been flipped to comply with ETABS API, asymmetric sections will suffer");
            }

#endif

            string stNodeId = GetAdapterId<string>(bhBar.Start);
            string endNodeId = GetAdapterId<string>(bhBar.End);

            if (string.IsNullOrEmpty(stNodeId) || string.IsNullOrEmpty(endNodeId))
            {
                Engine.Base.Compute.RecordError("Could not find the ids for at least one end node for at least one Bar. Bar not created.");
                return false;
            }

            ret = m_model.FrameObj.AddByPoint(stNodeId, endNodeId, ref name);


            if (ret != 0)
            {
                CreateElementError("Bar", name);
                return false;
            }

            //Label and story
            string label = "";
            string story = "";
            string guid = null;

            ETABSId etabsIdFragment = new ETABSId { Id = name };

            if (m_model.FrameObj.GetLabelFromName(name, ref label, ref story) == 0)
            {
                etabsIdFragment.Label = label;
                etabsIdFragment.Story = story;
            }

            if (m_model.AreaObj.GetGUID(name, ref guid) == 0)
                etabsIdFragment.PersistentId = guid;

            bhBar.SetAdapterId(etabsIdFragment);

            return SetObject(bhBar);
        }

        /***************************************************/

        [Description("Does all the ETABS interaction which does not initiate a new object in ETABS.")]
        private bool SetObject(Bar bhBar)
        {
            int ret = 0;
            string name = GetAdapterId<string>(bhBar);

            // Needed rigth after create as well as AddByPoint flipps the Bar if it feels like it
#if Debug16 == false && Release16 == false
            m_model.EditFrame.ChangeConnectivity(name, GetAdapterId<string>(bhBar.Start), GetAdapterId<string>(bhBar.End));
#endif

            if (CheckPropertyWarning(bhBar, b => b.SectionProperty))
            {
                string sectionName = GetAdapterId<string>(bhBar.SectionProperty);
                if (string.IsNullOrEmpty(sectionName) || m_model.FrameObj.SetSection(name, sectionName) != 0)
                {
                    CreatePropertyWarning("SectionProperty", "Bar", name);
                    ret++;
                }
            }

            if (m_model.FrameObj.SetLocalAxes(name, bhBar.OrientationAngle * 180 / System.Math.PI) != 0)
            {
                CreatePropertyWarning("Orientation angle", "Bar", name);
                ret++;
            }

            Offset offset = bhBar.Offset;

            double[] offset1 = new double[3];
            double[] offset2 = new double[3];

            if (offset != null && offset.Start != null && offset.End == null)
            {
                offset1[1] = offset.Start.Z;
                offset1[2] = offset.Start.Y;
                offset2[1] = offset.End.Z;
                offset2[2] = offset.End.Y;
            }

            // Avoid following operation if ETABS Version is ETABS21...
            string majorVersion = "";
            if(this.etabsVersion != null && this.etabsVersion.Contains("."))
                majorVersion = this.etabsVersion.Split('.')[0];

            if (majorVersion != "21") 
            {
                if (m_model.FrameObj.SetInsertionPoint(name, (int)bhBar.InsertionPoint(), false, 
                    bhBar.ModifyStiffnessInsertionPoint(), ref offset1, ref offset2) != 0)
                {
                    CreatePropertyWarning("Insertion point and perpendicular offset", "Bar", name);
                    ret++;
                }
            }

            if (bhBar.Release != null && bhBar.Release.StartRelease != null && bhBar.Release.EndRelease != null) 
            {
                bool[] restraintStart = null;
                double[] springStart = null;
                bool[] restraintEnd = null;
                double[] springEnd = null;

                if (bhBar.Release.ToCSI(ref restraintStart, ref springStart, ref restraintEnd, ref springEnd))
                {
                    if (m_model.FrameObj.SetReleases(name, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd) != 0)
                    {
                        CreatePropertyWarning("Release", "Bar", name);
                        ret++;
                    }
                }
            }

            AutoLengthOffset autoLengthOffset = bhBar.AutoLengthOffset();
            if (autoLengthOffset != null)
            {
                //the Rigid Zone Factor is not picked up when setting the auto length = true for the method call. Hence need to call this method twice.
                int retAutoLEngthOffset = m_model.FrameObj.SetEndLengthOffset(name, false, 0, 0, autoLengthOffset.RigidZoneFactor);
                retAutoLEngthOffset += m_model.FrameObj.SetEndLengthOffset(name, autoLengthOffset.AutoOffset, 0, 0, 0);
                if (retAutoLEngthOffset != 0)
                {
                    CreatePropertyWarning("Auto length offset", "Bar", name);
                    ret++;
                }
            }
            else if (offset != null && offset.Start != null && offset.End != null)
            {
                if (m_model.FrameObj.SetEndLengthOffset(name, false, bhBar.Offset.Start.X, -1 * (bhBar.Offset.End.X), 1) != 0)
                {
                    CreatePropertyWarning("Length offset", "Bar", name);
                    ret++;
                }
            }

            return true;
        }

        /***************************************************/

#if Debug16 || Release16

        [Description("Returns a bar where the endpoints have been flipped without cloning the object")]
        private static void FlipEndPoints(Bar bar)
        {
            // Flip the endpoints
            Node tempNode = bar.Start;
            bar.Start = bar.End;
            bar.End = tempNode;

            // Flip orientationAngle
            bar.OrientationAngle = -bar.OrientationAngle;
            if (bar.IsVertical())
                bar.OrientationAngle += Math.PI;

            // Flip Offsets
            if (bar.Offset != null)
            {
                Vector tempV = bar.Offset.Start;
                bar.Offset.Start = bar.Offset.End;
                bar.Offset.End = tempV;

                if(bar.Offset.Start != null)
                    bar.Offset.Start.X *= -1;

                if(bar.Offset.End != null)
                    bar.Offset.End.X *= -1;

                if (!bar.IsVertical())
                {
                    if(bar.Offset.Start != null)
                        bar.Offset.Start.Y *= -1;

                    if(bar.Offset.End != null)
                        bar.Offset.End.Y *= -1;
                }
            }
            // mirror the section 
            // not possible to push to ETABS afterwards if we did
            // warning for asymetric sections?

            // Flip Release
            if (bar.Release != null)
            {
                Constraint6DOF tempC = bar.Release.StartRelease;
                bar.Release.StartRelease = bar.Release.EndRelease;
                bar.Release.EndRelease = tempC;
            }
        }

        /***************************************************/

        private static void FlipInsertionPoint(Bar bar)
        {
            InsertionPoint fragment = bar.FindFragment<InsertionPoint>();
            if (fragment != null)
            {
                BarInsertionPoint insertionPoint = fragment.BarInsertionPoint;

                switch (insertionPoint)
                {
                    case BarInsertionPoint.BottomLeft:
                        fragment.BarInsertionPoint = BarInsertionPoint.BottomRight;
                        break;
                    case BarInsertionPoint.BottomRight:
                        fragment.BarInsertionPoint = BarInsertionPoint.BottomLeft;
                        break;
                    case BarInsertionPoint.MiddleLeft:
                        fragment.BarInsertionPoint = BarInsertionPoint.MiddleRight;
                        break;
                    case BarInsertionPoint.MiddleRight:
                        fragment.BarInsertionPoint = BarInsertionPoint.MiddleLeft;
                        break;
                    case BarInsertionPoint.TopLeft:
                        fragment.BarInsertionPoint = BarInsertionPoint.TopRight;
                        break;
                    case BarInsertionPoint.TopRight:
                        fragment.BarInsertionPoint = BarInsertionPoint.TopLeft;
                        break;
                    default:
                        break;
                }
                bar.Fragments.AddOrReplace(fragment);
            }
        }
#endif
        /***************************************************/

    }
}






