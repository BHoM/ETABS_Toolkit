using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structural.Elements;
using BH.oM.Structural.Properties;
using BH.oM.Common.Materials;
using ETABS2016;
using BH.Engine.ETABS;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        protected override IEnumerable<IBHoMObject> Read(Type type, IList ids)
        {
            if (type == typeof(Node))
                return ReadNodes(ids as dynamic);
            else if (type == typeof(Bar))
                return ReadBars(ids as dynamic);
            else if (type == typeof(ISectionProperty) || type.GetInterfaces().Contains(typeof(ISectionProperty)))
                return ReadSectionProperties(ids as dynamic);
            else if (type == typeof(Material))
                return ReadMaterials(ids as dynamic);
            return null;//<--- returning null will throw error in replace method of BHOM_Adapter line 34: can't do typeof(null) - returning null does seem the most sensible to return though
        }

        private List<Node> ReadNodes(List<string> ids = null)
        {
            List<Node> nodeList = new List<Node>();

            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                model.PointObj.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            foreach (string id in ids)
            {
                Node bhNode = new Node();
                double x, y, z;
                x = y = z = 0;
                bool[] restraint = new bool[6];
                double[] spring = new double[6];

                model.PointObj.GetCoordCartesian(id, ref x, ref y, ref z);
                bhNode.Position = new oM.Geometry.Point() { X = x, Y = y, Z = z };
                bhNode.CustomData.Add(AdapterId, id);

                model.PointObj.GetRestraint(id, ref restraint);
                model.PointObj.SetSpring(id, ref spring);
                bhNode.Constraint = Helper.GetConstraint6DOF(restraint, spring);


                nodeList.Add(bhNode);
            }


            return nodeList;
        }

        private List<Bar> ReadBars(List<string> ids = null)
        {
            List<Bar> barList = new List<Bar>();
            int nameCount = 0;
            string[] names = { };

            if (ids == null)
            {
                model.FrameObj.GetNameList(ref nameCount, ref names);
                ids = names.ToList();
            }

            foreach (string id in ids)
            {
                Bar bhBar = new Bar();
                bhBar.CustomData.Add(AdapterId, id);
                string startId = "";
                string endId = "";
                model.FrameObj.GetPoints(id, ref startId, ref endId);

                List<Node> endNodes = ReadNodes(new List<string> { startId, endId });
                bhBar.StartNode = endNodes[0];
                bhBar.EndNode = endNodes[1];

                bool[] restraintStart = new bool[6];
                double[] springStart = new double[6];
                bool[] restraintEnd = new bool[6];
                double[] springEnd = new double[6];

                model.FrameObj.GetReleases(id, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd);
                bhBar.Release = new BarRelease();
                bhBar.Release.StartRelease = Helper.GetConstraint6DOF(restraintStart, springStart);
                bhBar.Release.EndRelease = Helper.GetConstraint6DOF(restraintEnd, springEnd);

                eFramePropType propertyType = eFramePropType.General;
                string propertyName = "";
                string sAuto = "";
                model.FrameObj.GetSection(id, ref propertyName, ref sAuto);
                model.PropFrame.GetTypeOAPI(propertyName, ref propertyType);
                bhBar.SectionProperty = Helper.GetSectionProperty(model, propertyName, propertyType);

                barList.Add(bhBar);
            }
            return barList;
        }

        private List<ISectionProperty> ReadSectionProperties(List<string> ids = null)
        {
            List<ISectionProperty> propList = new List<ISectionProperty>();
            int nameCount = 0;
            string[] names = { };

            if (ids == null)
            {
                model.PropFrame.GetNameList(ref nameCount, ref names);
                ids = names.ToList();
            }

            eFramePropType propertyType = eFramePropType.General;

            foreach (string id in ids)
            {
                model.PropFrame.GetTypeOAPI(id, ref propertyType);
                propList.Add(Helper.GetSectionProperty(model, id, propertyType));
            }
            return propList;
        }

        private List<Material> ReadMaterials(List<string> ids = null)
        {
            int nameCount = 0;
            string[] names = { };
            List<Material> materialList = new List<Material>();

            if (ids == null)
            {
                model.PropMaterial.GetNameList(ref nameCount, ref names);
                ids = names.ToList();
            }

            foreach (string id in ids)
            {
                materialList.Add(Helper.GetMaterial(model, id));
            }

            return materialList;
        }

        private List<Property2D> ReadProperty2d(List<string> ids = null)
        {
            List<Property2D> propertyList = new List<Property2D>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                model.PropArea.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            foreach (string id in ids)
            {
                Property2D bhProperty = new Property2D();
            }


            return propertyList;
        }

        private List<PanelPlanar> ReadPanel(List<string> ids = null)
        {
            List<PanelPlanar> panelList = new List<PanelPlanar>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                model.AreaObj.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }



            return panelList;
        }


    }
}
