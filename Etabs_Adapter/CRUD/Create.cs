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

using System.Collections.Generic;
using System.Linq;
using BH.oM.Architecture.Elements;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Loads;
using BH.oM.Structure.Offsets;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;
using BH.oM.Adapters.ETABS.Elements;
#if Debug2017
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {

        /***************************************************/
        protected override bool Create<T>(IEnumerable<T> objects, bool replaceAll = false)
        {
            bool success = true;

            if (typeof(BH.oM.Base.IBHoMObject).IsAssignableFrom(typeof(T)))
            {
                success = CreateCollection(objects);
            }
            else
            {
                success = false;
            }

            m_model.View.RefreshView();
            return success;
        }

        /***************************************************/

        private bool CreateCollection<T>(IEnumerable<T> objects) where T : BH.oM.Base.IObject
        {
            bool success = true;

            if (typeof(T) == typeof(Panel))
            {
                List<Panel> panels = objects.Cast<Panel>().ToList();

                List<Diaphragm> diaphragms = panels.Select(x => x.Diaphragm()).Where(x => x != null).ToList();

                this.Replace(diaphragms);
            }

            if (typeof(T) == typeof(Level))
            {
                return CreateCollection(objects as IEnumerable<Level>);
            }
            else
            {
                foreach (T obj in objects)
                {
                    success &= CreateObject(obj as dynamic);
                }
            }
            return success;
        }

        /***************************************************/

        private bool CreateObject(Node bhNode)
        {
            bool success = true;
            int retA = 0;
            int retB = 0;
            int retC = 0;

            string name = "";
            string bhId = bhNode.CustomData[AdapterId].ToString();
            name = bhId;

            oM.Geometry.Point position = bhNode.Position();
            retA = m_model.PointObj.AddCartesian(position.X, position.Y, position.Z, ref name);

            if (name != bhId)
                bhNode.CustomData[AdapterId] = name;
            //if (name != bhId)
            //    success = false; //this is not necessary if you can guarantee that it is impossible that this bhId does not match any existing name in ETABS !!!

            if (bhNode.Support != null)
            {
                bool[] restraint = new bool[6];
                restraint[0] = bhNode.Support.TranslationX == DOFType.Fixed;
                restraint[1] = bhNode.Support.TranslationY == DOFType.Fixed;
                restraint[2] = bhNode.Support.TranslationZ == DOFType.Fixed;
                restraint[3] = bhNode.Support.RotationX == DOFType.Fixed;
                restraint[4] = bhNode.Support.RotationY == DOFType.Fixed;
                restraint[5] = bhNode.Support.RotationZ == DOFType.Fixed;

                double[] spring = new double[6];
                spring[0] = bhNode.Support.TranslationalStiffnessX;
                spring[1] = bhNode.Support.TranslationalStiffnessY;
                spring[2] = bhNode.Support.TranslationalStiffnessZ;
                spring[3] = bhNode.Support.RotationalStiffnessX;
                spring[4] = bhNode.Support.RotationalStiffnessY;
                spring[5] = bhNode.Support.RotationalStiffnessZ;

                retB = m_model.PointObj.SetRestraint(name, ref restraint);
                retC = m_model.PointObj.SetSpring(name, ref spring);
            }

            if (retA != 0 || retB != 0 || retC != 0)
                success = false;

            return success;
        }

        /***************************************************/

        private bool CreateObject(Bar bhBar)
        {
            bool success = true;
            int ret = 0;


            string name = "";
            string bhId = bhBar.CustomData[AdapterId].ToString();
            name = bhId;
            
            ret = m_model.FrameObj.AddByPoint(bhBar.StartNode.CustomData[AdapterId].ToString(), bhBar.EndNode.CustomData[AdapterId].ToString(), ref name);

            if (ret != 0)
            {
                CreateElementError("Bar", name);
                return false;
            }

            if (m_model.FrameObj.SetSection(name, bhBar.SectionProperty.Name) != 0)
            {
                CreatePropertyWarning("SectionProperty", "Bar", name);
                ret++;
            }

            if (m_model.FrameObj.SetLocalAxes(name, bhBar.OrientationAngle * 180 / System.Math.PI) != 0)
            {
                CreatePropertyWarning("Orientation angle", "Bar", name);
                ret++;
            }

            Offset offset = bhBar.Offset;

            double[] offset1 = new double[3];
            double[] offset2 = new double[3];

            if (offset != null)
            {
                offset1[1] = offset.Start.Z;
                offset1[2] = offset.Start.Y;
                offset2[1] = offset.End.Z;
                offset2[2] = offset.End.Y;
            }

            if (m_model.FrameObj.SetInsertionPoint(name, (int)bhBar.InsertionPoint(), false, true, ref offset1, ref offset2) != 0)
            {
                CreatePropertyWarning("insertion point and perpendicular offset", "Bar", name);
                ret++;
            }

            BarRelease barRelease = bhBar.Release;
            if (barRelease != null)
            {
                bool[] restraintStart;// = barRelease.StartRelease.Fixities();// Helper.GetRestraint6DOF(barRelease.StartRelease);
                double[] springStart;// = barRelease.StartRelease.ElasticValues();// Helper.GetSprings6DOF(barRelease.StartRelease);
                bool[] restraintEnd;// = barRelease.EndRelease.Fixities();// Helper.GetRestraint6DOF(barRelease.EndRelease);
                double[] springEnd;// = barRelease.EndRelease.ElasticValues();// Helper.GetSprings6DOF(barRelease.EndRelease);


                GetBarReleaseArrays(barRelease, out restraintStart, out restraintEnd, out springStart, out springEnd);

                if (m_model.FrameObj.SetReleases(name, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd) != 0)
                {
                    CreatePropertyWarning("Release", "Bar", name);
                    ret++;
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
            else if (bhBar.Offset != null)
            {
                if (m_model.FrameObj.SetEndLengthOffset(name, false, -1 * (bhBar.Offset.Start.X), bhBar.Offset.End.X, 1) != 0)
                {
                    CreatePropertyWarning("Length offset", "Bar", name);
                    ret++;
                }
            }


            return ret == 0;
        }

        /***************************************************/

        private bool CreateObject(ISectionProperty bhSection)
        {
            bool success = true;
            
            Helper.SetSectionProperty(m_model, bhSection);//TODO: this is only halfway done - should be moved away from engine to adapter as much as possible
            return success;
        }

        /***************************************************/

        private bool CreateObject(IMaterialFragment material)
        {
            bool success = true;
            
            Helper.SetMaterial(m_model, material); //TODO: this is only halfway done - should be moved away from engine to adapter as much as possible

            return success;

        }

        /***************************************************/

        private bool CreateObject(ISurfaceProperty property2d)
        {
            bool success = true;
            int retA = 0;

            string propertyName = property2d.Name;// property2d.CustomData[AdapterId].ToString();

            eShellType shellType = property2d.EtabsShellType();

            if (property2d.GetType() == typeof(Waffle))
            {
                Waffle waffleProperty = (Waffle)property2d;
                m_model.PropArea.SetSlab(propertyName, eSlabType.Waffle, shellType, property2d.Material.Name, waffleProperty.Thickness);
                retA = m_model.PropArea.SetSlabWaffle(propertyName, waffleProperty.TotalDepthX, waffleProperty.Thickness, waffleProperty.StemWidthX, waffleProperty.StemWidthX, waffleProperty.SpacingX, waffleProperty.SpacingY);
            }
            else if (property2d.GetType() == typeof(Ribbed))
            {
                Ribbed ribbedProperty = (Ribbed)property2d;
                m_model.PropArea.SetSlab(propertyName, eSlabType.Ribbed, shellType, property2d.Material.Name, ribbedProperty.Thickness);
                retA = m_model.PropArea.SetSlabRibbed(propertyName, ribbedProperty.TotalDepth, ribbedProperty.Thickness, ribbedProperty.StemWidth, ribbedProperty.StemWidth, ribbedProperty.Spacing, (int)ribbedProperty.Direction);
            }
            else if (property2d.GetType() == typeof(LoadingPanelProperty))
            {
                retA = m_model.PropArea.SetSlab(propertyName, eSlabType.Slab, shellType, property2d.Material.Name, 0);
            }

            else if (property2d.GetType() == typeof(ConstantThickness))
            {
                ConstantThickness constantThickness = (ConstantThickness)property2d;
                if (constantThickness.PanelType == PanelType.Wall)
                    retA = m_model.PropArea.SetWall(propertyName, eWallPropType.Specified, shellType, property2d.Material.Name, constantThickness.Thickness);
                else
                    retA = m_model.PropArea.SetSlab(propertyName, eSlabType.Slab, shellType, property2d.Material.Name, constantThickness.Thickness);
            }


            if (property2d.HasModifiers())
            {
                double[] modifier = property2d.Modifiers();//(double[])property2d.CustomData["Modifiers"];
                m_model.PropArea.SetModifiers(propertyName, ref modifier);
            }

            if (retA != 0)
                success = false;

            return success;
        }

        /***************************************************/

        private bool CreateObject(Panel bhPanel)
        {
            bool success = true;
            int retA = 0;

            double mergeTol = 1e-3; //Merging panel points to the mm, same behaviour as the default node comparer

            string name = bhPanel.CustomData[AdapterId].ToString();
            string propertyName = bhPanel.Property.Name;
            List<BH.oM.Geometry.Point> boundaryPoints = bhPanel.ControlPoints(true).CullDuplicates(mergeTol);

            int segmentCount = boundaryPoints.Count();
            double[] x = new double[segmentCount];
            double[] y = new double[segmentCount];
            double[] z = new double[segmentCount];
            for(int i=0; i < segmentCount;i++)
            {
                x[i] = boundaryPoints[i].X;
                y[i] = boundaryPoints[i].Y;
                z[i] = boundaryPoints[i].Z;
            }

            retA = m_model.AreaObj.AddByCoord(segmentCount, ref x, ref y, ref z, ref name, propertyName);

            if (retA != 0)
                return false;

            if (bhPanel.Openings != null)
            {
                for(int i=0;i < bhPanel.Openings.Count;i++)
                {
                    boundaryPoints = bhPanel.Openings[i].ControlPoints().CullDuplicates(mergeTol);

                    segmentCount = boundaryPoints.Count();
                    x = new double[segmentCount];
                    y = new double[segmentCount];
                    z = new double[segmentCount];

                    for (int j = 0; j < segmentCount; j++)
                    {
                        x[j] = boundaryPoints[j].X;
                        y[j] = boundaryPoints[j].Y;
                        z[j] = boundaryPoints[j].Z;
                    }

                    string openingName = name + "_Opening_" + i;
                    m_model.AreaObj.AddByCoord(segmentCount, ref x, ref y, ref z, ref openingName,"");//<-- setting panel property to empty string, verify that this is correct
                    m_model.AreaObj.SetOpening(openingName, true);
                }
            }


            Diaphragm diaphragm = bhPanel.Diaphragm();

            if (diaphragm != null)
            {
                m_model.AreaObj.SetDiaphragm(name, diaphragm.Name);
            }

            return success;
        }


        /***************************************************/

        private bool CreateObject(Diaphragm diaphragm)
        {
            bool sucess = true;
            sucess &= m_model.Diaphragm.SetDiaphragm(diaphragm.Name, diaphragm.Rigidity == oM.Adapters.ETABS.DiaphragmType.SemiRigidDiaphragm) == 0;

            return sucess;
        }

        /***************************************************/

        private bool CreateObject(Loadcase loadcase)
        {
            bool success = true;

            Helper.SetLoadcase(m_model, loadcase);

            return success;
        }

        /***************************************************/

        private bool CreateObject(MassSource massSource)
        {

            bool includeElements = massSource.ElementSelfMass;
            bool includeAddMass = massSource.AdditionalMass;
            bool includeLoads = massSource.FactoredAdditionalCases.Count > 0;

            int count = massSource.FactoredAdditionalCases.Count;
            string[] cases = new string[count];
            double[] factors = new double[count];

            for (int i = 0; i < count; i++)
            {
                cases[i] = Helper.CaseNameToCSI(massSource.FactoredAdditionalCases[i].Item1);
                factors[i] = massSource.FactoredAdditionalCases[i].Item2;
            }

            return m_model.PropMaterial.SetMassSource_1(ref includeElements, ref includeAddMass, ref includeLoads, count, ref cases, ref factors) == 0;
        }

        /***************************************************/

        private bool CreateObject(ModalCase modalCase)
        {
            bool success = true;

            return false;
        }

        /***************************************************/

        private bool CreateObject(LoadCombination loadcombination)
        {
            bool success = true;

            Helper.SetLoadCombination(m_model, loadcombination);

            return success;
        }

        /***************************************************/

        private bool CreateObject(ILoad bhLoad)
        {
            bool success = true;

            Helper.SetLoad(m_model, bhLoad as dynamic, this.EtabsConfig.ReplaceLoads);


            return success;
        }

        /***************************************************/

        private bool CreateObject(RigidLink bhLink)
        {
            bool success = true;
            int retA = 0;
            int retB = 0;

            string name = "";
            string givenName = "";
            string bhId = bhLink.CustomData[AdapterId].ToString();
            name = bhId;

            LinkConstraint constraint = bhLink.Constraint;//not used yet
            Node masterNode = bhLink.MasterNode;
            List<Node> slaveNodes = bhLink.SlaveNodes;
            bool multiSlave = slaveNodes.Count() == 1 ? false : true;

            //double XI = masterNode.Position.X;
            //double YI = masterNode.Position.Y;
            //double ZI = masterNode.Position.Z;

            for(int i=0; i<slaveNodes.Count();i++)
            {
                //double XJ = slaveNodes[i].Position.X * 1000;//multiply by 1000 to compensate for Etabs strangeness: yes, one end is divided by 1000 the other end is not!
                //double YJ = slaveNodes[i].Position.Y * 1000;
                //double ZJ = slaveNodes[i].Position.Z * 1000;

                name = multiSlave == true ? name + ":::" + i : name;

                //retA = model.LinkObj.AddByCoord(XI, YI, ZI, XJ, YJ, ZJ, ref givenName, false, "Default", name);
                retA = m_model.LinkObj.AddByPoint(masterNode.CustomData[AdapterId].ToString(), slaveNodes[i].CustomData[AdapterId].ToString(), ref givenName, false, constraint.Name, name);

            }

            return success;
        }

        /***************************************************/

        private bool CreateObject(LinkConstraint bhLinkConstraint)
        {

            string name = bhLinkConstraint.Name;

            bool[] dof = new bool[6];

            for (int i = 0; i < 6; i++)
                dof[i] = true;

            bool[] fix = new bool[6];

            fix[0] = bhLinkConstraint.XtoX;
            fix[1] = bhLinkConstraint.ZtoZ;
            fix[2] = bhLinkConstraint.YtoY;
            fix[3] = bhLinkConstraint.XXtoXX;
            fix[4] = bhLinkConstraint.ZZtoZZ;
            fix[5] = bhLinkConstraint.YYtoYY;

            double[] stiff = new double[6];
            double[] damp = new double[6];

            int ret = m_model.PropLink.SetLinear(name, ref dof, ref fix, ref stiff, ref damp, 0, 0);

            if (ret != 0)
                CreateElementError("Link Constraint", name);

            return ret == 0;

        }



        /***************************************************/

        private bool CreateCollection(IEnumerable<Level> levels)
        {
            int count = levels.Count();
            if (count < 1)
                return true;

            List<Level> levelList = levels.OrderBy(x => x.Elevation).ToList();

            string[] names = levelList.Select(x => x.Name).ToArray();
            double[] elevations = new double[count + 1];

            for (int i = 0; i < count; i++)
            {
                elevations[i + 1] = levelList[i].Elevation;
            }

            double[] heights = new double[count];   //Heihgts empty, set by elevations
            bool[] isMasterStory = new bool[count];
            isMasterStory[count - 1] = true;    //Top story as master
            string[] similarTo = new string[count];
            for (int i = 0; i < count; i++)
            {
                similarTo[i] = "";  //No similarities
            }

            bool[] spliceAbove = new bool[count];   //No splice
            double[] spliceHeight = new double[count];  //No splice



            int ret = m_model.Story.SetStories(names, elevations, heights, isMasterStory, similarTo, spliceAbove, spliceHeight);

            if (ret != 0)
            {
                Engine.Reflection.Compute.RecordError("Failed to push levels. Levels can only be pushed to an empty model.");
            }

            return ret == 0;

        }

        /***************************************************/

        private void GetBarReleaseArrays(BarRelease release, out bool[] startFixities, out bool[] endFixities, out double[] startValues, out double[] endValues)
        {
            GetOneSideBarReleaseArrays(release.StartRelease, out startFixities, out startValues);
            GetOneSideBarReleaseArrays(release.EndRelease, out endFixities, out endValues);
        }

        /***************************************************/

        private void GetOneSideBarReleaseArrays(Constraint6DOF constraint, out bool[] fixities, out double[] values)
        {
            fixities = new bool[6];

            //Note: Etabs does not follow the same convention as the BHoM. Different notation is also used
            //where 1,2 and 3 is used instead of X,Y, and Z. The convention is as follows: 1 = X, 2 = Z and 3 = Y in etabs

            fixities[0] = constraint.TranslationX != DOFType.Fixed;
            fixities[1] = constraint.TranslationZ != DOFType.Fixed;
            fixities[2] = constraint.TranslationY != DOFType.Fixed;

            fixities[3] = constraint.RotationX != DOFType.Fixed;
            fixities[4] = constraint.RotationZ != DOFType.Fixed;    
            fixities[5] = constraint.RotationY != DOFType.Fixed;   

            values = new double[6];

            values[0] = constraint.TranslationalStiffnessX;
            values[1] = constraint.TranslationalStiffnessZ;
            values[2] = constraint.TranslationalStiffnessY;


            values[3] = constraint.RotationalStiffnessX;
            values[4] = constraint.RotationalStiffnessZ;
            values[5] = constraint.RotationalStiffnessY;

        }

        /***************************************************/

        private void CreateElementError(string elemType, string elemName)
        {
            Engine.Reflection.Compute.RecordError("Failed to create the element of type " + elemType + ", with id: " + elemName);
        }

        /***************************************************/

        private void CreatePropertyError(string failedProperty, string elemType, string elemName)
        {
            CreatePropertyEvent(failedProperty, elemType, elemName, oM.Reflection.Debugging.EventType.Error);
        }

        /***************************************************/

        private void CreatePropertyWarning(string failedProperty, string elemType, string elemName)
        {
            CreatePropertyEvent(failedProperty, elemType, elemName, oM.Reflection.Debugging.EventType.Warning);
        }

        /***************************************************/

        private void CreatePropertyEvent(string failedProperty, string elemType, string elemName, oM.Reflection.Debugging.EventType eventType)
        {
            Engine.Reflection.Compute.RecordEvent("Failed to set property " + failedProperty + " for the " + elemType + "with id: " + elemName, eventType);
        }

        /***************************************************/
    }
}
