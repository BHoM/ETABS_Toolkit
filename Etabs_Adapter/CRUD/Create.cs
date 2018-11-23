using System.Collections.Generic;
using System.Linq;
using BH.oM.Architecture.Elements;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Properties;
using BH.oM.Structure.Loads;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Common.Materials;
using BH.Engine.ETABS;
using BH.oM.Adapters.ETABS.Elements;

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

            if (typeof(T) == typeof(PanelPlanar))
            {
                List<PanelPlanar> panels = objects.Cast<PanelPlanar>().ToList();

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

            retA = m_model.PointObj.AddCartesian(bhNode.Position.X, bhNode.Position.Y, bhNode.Position.Z, ref name);

            if (name != bhId)
                bhNode.CustomData[AdapterId] = name;
            //if (name != bhId)
            //    success = false; //this is not necessary if you can guarantee that it is impossible that this bhId does not match any existing name in ETABS !!!

            if (bhNode.Constraint != null)
            {
                bool[] restraint = new bool[6];
                restraint[0] = bhNode.Constraint.TranslationX == DOFType.Fixed;
                restraint[1] = bhNode.Constraint.TranslationY == DOFType.Fixed;
                restraint[2] = bhNode.Constraint.TranslationZ == DOFType.Fixed;
                restraint[3] = bhNode.Constraint.RotationX == DOFType.Fixed;
                restraint[4] = bhNode.Constraint.RotationY == DOFType.Fixed;
                restraint[5] = bhNode.Constraint.RotationZ == DOFType.Fixed;

                double[] spring = new double[6];
                spring[0] = bhNode.Constraint.TranslationalStiffnessX;
                spring[1] = bhNode.Constraint.TranslationalStiffnessY;
                spring[2] = bhNode.Constraint.TranslationalStiffnessZ;
                spring[3] = bhNode.Constraint.RotationalStiffnessX;
                spring[4] = bhNode.Constraint.RotationalStiffnessY;
                spring[5] = bhNode.Constraint.RotationalStiffnessZ;

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
                bool[] restraintStart = barRelease.StartRelease.Fixities();// Helper.GetRestraint6DOF(barRelease.StartRelease);
                double[] springStart = barRelease.StartRelease.ElasticValues();// Helper.GetSprings6DOF(barRelease.StartRelease);
                bool[] restraintEnd = barRelease.EndRelease.Fixities();// Helper.GetRestraint6DOF(barRelease.EndRelease);
                double[] springEnd = barRelease.EndRelease.ElasticValues();// Helper.GetSprings6DOF(barRelease.EndRelease);

                if (m_model.FrameObj.SetReleases(name, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd) != 0)
                {
                    CreatePropertyWarning("Release", "Bar", name);
                    ret++;
                }
            }

            if (bhBar.AutoLengthOffset())
            {
                if (m_model.FrameObj.SetEndLengthOffset(name, true, 0, 0, 0) != 0)
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

        private bool CreateObject(Material material)
        {
            bool success = true;
            
            Helper.SetMaterial(m_model, material); //TODO: this is only halfway done - should be moved away from engine to adapter as much as possible

            return success;

        }

        /***************************************************/

        private bool CreateObject(IProperty2D property2d)
        {
            bool success = true;
            int retA = 0;

            string propertyName = property2d.Name;// property2d.CustomData[AdapterId].ToString();


            if (property2d.GetType() == typeof(Waffle))
            {
                Waffle waffleProperty = (Waffle)property2d;
                retA = m_model.PropArea.SetSlabWaffle(propertyName, waffleProperty.TotalDepthX, waffleProperty.Thickness, waffleProperty.StemWidthX, waffleProperty.StemWidthX, waffleProperty.SpacingX, waffleProperty.SpacingY);
            }

            if (property2d.GetType() == typeof(Ribbed))
            {
                Ribbed ribbedProperty = (Ribbed)property2d;
                retA = m_model.PropArea.SetSlabRibbed(propertyName, ribbedProperty.TotalDepth, ribbedProperty.Thickness, ribbedProperty.StemWidth, ribbedProperty.StemWidth, ribbedProperty.Spacing, (int)ribbedProperty.Direction);
            }

            if (property2d.GetType() == typeof(LoadingPanelProperty))
            {
                retA = m_model.PropArea.SetSlab(propertyName, ETABS2016.eSlabType.Slab, ETABS2016.eShellType.ShellThin, property2d.Material.Name, 0);
            }

            if (property2d.GetType() == typeof(ConstantThickness))
            {
                ConstantThickness constantThickness = (ConstantThickness)property2d;
                if (constantThickness.PanelType == PanelType.Wall)
                    retA = m_model.PropArea.SetWall(propertyName, ETABS2016.eWallPropType.Specified, ETABS2016.eShellType.ShellThin, property2d.Material.Name, constantThickness.Thickness);
                else
                    retA = m_model.PropArea.SetSlab(propertyName, ETABS2016.eSlabType.Slab, ETABS2016.eShellType.ShellThin, property2d.Material.Name, constantThickness.Thickness);
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

        private bool CreateObject(PanelPlanar bhPanel)
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

        private void CreateElementError(string elemType, string elemName)
        {
            Engine.Reflection.Compute.RecordError("Failed to create the element of type " + elemType + ", with id: " + elemName);
        }

        /***************************************************/

        private void CreatePropertyError(string failedProperty, string elemType, string elemName)
        {
            CreatePropertyEvent(failedProperty, elemType, elemName, oM.Reflection.Debuging.EventType.Error);
        }

        /***************************************************/

        private void CreatePropertyWarning(string failedProperty, string elemType, string elemName)
        {
            CreatePropertyEvent(failedProperty, elemType, elemName, oM.Reflection.Debuging.EventType.Warning);
        }

        /***************************************************/

        private void CreatePropertyEvent(string failedProperty, string elemType, string elemName, oM.Reflection.Debuging.EventType eventType)
        {
            Engine.Reflection.Compute.RecordEvent("Failed to set property " + failedProperty + " for the " + elemType + "with id: " + elemName, eventType);
        }

        /***************************************************/
    }
}
