using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.Engine.ETABS;
using BH.oM;
using BH.oM.Structural;
using BH.oM.Structural.Elements;
using BH.oM.Structural.Properties;
using BH.oM.Structural.Loads;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Common.Materials;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        protected override bool Create<T>(IEnumerable<T> objects, bool replaceAll = false)
        {
            bool success = true;

            if (typeof(BH.oM.Base.IBHoMObject).IsAssignableFrom(typeof(T)))
            {
                foreach (T obj in objects)
                {
                    success = CreateObject(obj as dynamic);
                    //if (!success)
                    //    break;
                    //((BH.oM.Base.IBHoMObject)obj).ToETABS(modelData);
                }
            }
            else
            {
                success = false;
            }

            model.View.RefreshView();
            return success;
        }

        private bool CreateObject(Node bhNode)
        {
            bool success = true;
            int retA = 0;
            int retB = 0;
            int retC = 0;

            string name = "";
            string bhId = bhNode.CustomData[AdapterId].ToString();
            name = bhId;

            retA = model.PointObj.AddCartesian(bhNode.Position.X, bhNode.Position.Y, bhNode.Position.Z, ref name);
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

                retB = model.PointObj.SetRestraint(name, ref restraint);
                retC = model.PointObj.SetSpring(name, ref spring);
            }

            if (retA != 0 || retB != 0 || retC != 0)
                success = false;

            return success;
        }

        private bool CreateObject(Bar bhBar)
        {
            bool success = true;
            int retA = 0;
            int retB = 0;
            int retC = 0;

            string name = "";
            string bhId = bhBar.CustomData[AdapterId].ToString();
            name = bhId;

            retA = model.FrameObj.AddByPoint(bhBar.StartNode.CustomData[AdapterId].ToString(), bhBar.EndNode.CustomData[AdapterId].ToString(), ref name);
            //if (bhId != name)
            //    success = false;

            //model.FrameObj.SetGUID(name, bhNode.TaggedName());// see comment on node convert
            retB = model.FrameObj.SetSection(name, bhBar.SectionProperty.Name);
            //model.FrameObj.SetReleases();
            BarRelease barRelease = bhBar.Release;
            if (barRelease != null)
            {
                bool[] restraintStart = Helper.GetRestraint6DOF(barRelease.StartRelease);
                double[] springStart = Helper.GetSprings6DOF(barRelease.StartRelease);
                bool[] restraintEnd = Helper.GetRestraint6DOF(barRelease.EndRelease);
                double[] springEnd = Helper.GetSprings6DOF(barRelease.EndRelease);

                model.FrameObj.SetReleases(name, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd);
            }

            //model.FrameObj.SetGroupAssign();
            if (retA != 0 || retB != 0 || retC != 0)
                success = false;

            return success;
        }

        private bool CreateObject(ISectionProperty bhSection)
        {
            bool success = true;

            Helper.SetSectionProperty(model, bhSection);//TODO: this is only halfway done - should be moved away from engine to adapter as much as possible

            return success;
        }

        private bool CreateObject(Material material)
        {
            bool success = true;

            Helper.SetMaterial(model, material); //TODO: this is only halfway done - should be moved away from engine to adapter as much as possible

            return success;
        }

        private bool CreateObject(IProperty2D property2d)
        {
            bool success = true;
            int retA = 0;

            string propertyName = property2d.Name;// property2d.CustomData[AdapterId].ToString();


            if (property2d.GetType() == typeof(Waffle))
            {
                Waffle waffleProperty = (Waffle)property2d;
                retA = model.PropArea.SetSlabWaffle(propertyName, waffleProperty.TotalDepthX, waffleProperty.Thickness, waffleProperty.StemWidthX, waffleProperty.StemWidthX, waffleProperty.SpacingX, waffleProperty.SpacingY);
            }

            if (property2d.GetType() == typeof(Ribbed))
            {
                Ribbed ribbedProperty = (Ribbed)property2d;
                retA = model.PropArea.SetSlabRibbed(propertyName, ribbedProperty.TotalDepth, ribbedProperty.Thickness, ribbedProperty.StemWidth, ribbedProperty.StemWidth, ribbedProperty.Spacing, (int)ribbedProperty.Direction);
            }

            if (property2d.GetType() == typeof(LoadingPanelProperty))
            {
                retA = model.PropArea.SetSlab(propertyName, ETABS2016.eSlabType.Slab, ETABS2016.eShellType.ShellThin, property2d.Material.Name, 0);
            }

            if (property2d.GetType() == typeof(ConstantThickness))
            {
                ConstantThickness constantThickness = (ConstantThickness)property2d;
                if (constantThickness.PanelType == PanelType.Wall)
                    retA = model.PropArea.SetWall(propertyName, ETABS2016.eWallPropType.Specified, ETABS2016.eShellType.ShellThin, property2d.Material.Name, constantThickness.Thickness);
                else
                    retA = model.PropArea.SetSlab(propertyName, ETABS2016.eSlabType.Slab, ETABS2016.eShellType.ShellThin, property2d.Material.Name, constantThickness.Thickness);
            }


            if (property2d.HasModifiers())
            {
                double[] modifier = property2d.Modifiers();//(double[])property2d.CustomData["Modifiers"];
                model.PropArea.SetModifiers(propertyName, ref modifier);
            }

            if (retA != 0)
                success = false;

            return success;
        }

        private bool CreateObject(PanelPlanar bhPanel)
        {
            bool success = true;
            int retA = 0;
            
            string name = bhPanel.CustomData[AdapterId].ToString();
            string propertyName = bhPanel.Property.Name;
            List<BH.oM.Geometry.Point> boundaryPoints = new List<oM.Geometry.Point>();

            foreach(Edge edge in bhPanel.ExternalEdges)
                boundaryPoints.AddRange(edge.Curve.IControlPoints());
            boundaryPoints = boundaryPoints.Distinct().ToList();

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

            retA = model.AreaObj.AddByCoord(segmentCount, ref x, ref y, ref z, ref name, propertyName);
            if (retA != 0)
                return false;

            if (bhPanel.Openings != null)
            {
                for(int i=0;i < bhPanel.Openings.Count;i++)
                {
                    boundaryPoints = new List<oM.Geometry.Point>();
                    foreach(Edge edge in bhPanel.Openings[i].Edges)
                        boundaryPoints.AddRange(edge.Curve.IControlPoints());
                    boundaryPoints = boundaryPoints.Distinct().ToList();

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
                    model.AreaObj.AddByCoord(segmentCount, ref x, ref y, ref z, ref openingName,"");//<-- setting panel property to empty string, verify that this is correct
                    model.AreaObj.SetOpening(openingName, true);
                }
            }

            return success;
        }

        private bool CreateObject(Loadcase loadcase)
        {
            bool success = true;

            Helper.SetLoadcase(model, loadcase);

            return success;
        }

        private bool CreateObject(LoadCombination loadcombination)
        {
            bool success = true;

            Helper.SetLoadCombination(model, loadcombination);

            return success;
        }


        private bool CreateObject(ILoad bhLoad)
        {
            bool success = true;

            Helper.SetLoad(model, bhLoad as dynamic);


            return success;
        }

    }
}
