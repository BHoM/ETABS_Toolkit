using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABS2016;
using BH.oM;
using BH.oM.Structural;
using BH.oM.Structural.Elements;
using BH.oM.Structural.Properties;

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        /// <summary>
        /// ids = null returns all nodes in model
        /// </summary>
        public static List<Node> ToBHoM(this cPointObj pointObj, List<string> ids)
        {
            List<Node> bhNodes = new List<Node>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                pointObj.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            foreach (string id in ids)
            {
                bhNodes.Add(pointObj.ToBHoM(id));
            }

            return bhNodes;
        }


        public static Node ToBHoM(this cPointObj pointObj, string id)
        {
            Node bhNode = new Node();
            double x, y, z;
            x = y = z = 0;
            bool[] restraint = new bool[6];
            double[] spring = new double[6];

            pointObj.GetCoordCartesian(id, ref x, ref y, ref z);
            bhNode.Position = new oM.Geometry.Point() { X = x, Y = y, Z = z };
            bhNode.CustomData.Add(AdapterId, id);

            pointObj.GetRestraint(id, ref restraint);
            pointObj.SetSpring(id, ref spring);
            bhNode.Constraint = GetConstraint6DOF(restraint, spring);

            return bhNode;
        }


        public static Bar ToBHoM(this cFrameObj barObj, string id, ModelData modelData)
        {
            Bar bhBar = new Bar();
            bhBar.CustomData.Add(AdapterId, id);
            string startId="";
            string endId="";
            barObj.GetPoints(id, ref startId, ref endId);

            //this method can only be called as 'model.frameObj.ToBHoM' 
            //still 'model' needs to be passed as argument as well in order to get the nodes at bar ends
            //this seems a bit convoluted way to keep to the convention of .ToBHoM extension methods !!!

            bhBar.StartNode = modelData.model.PointObj.ToBHoM(startId);
            bhBar.EndNode = modelData.model.PointObj.ToBHoM(endId);

            bool[] restraintStart = new bool[6];
            double[] springStart = new double[6];
            bool[] restraintEnd = new bool[6];
            double[] springEnd = new double[6];

            barObj.GetReleases(id, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd);
            bhBar.Release = new BarRelease();
            bhBar.Release.StartRelease = GetConstraint6DOF(restraintStart, springStart);
            bhBar.Release.EndRelease = GetConstraint6DOF(restraintEnd, springEnd);

            eFramePropType propertyType = eFramePropType.General;
            string propertyName = "";
            string sAuto = "";
            barObj.GetSection(id, ref propertyName, ref sAuto);
            modelData.model.PropFrame.GetTypeOAPI(propertyName, ref propertyType);
            bhBar.SectionProperty = GetSectionProperty(modelData, propertyName, propertyType);
            return bhBar;
        }

    }
}
