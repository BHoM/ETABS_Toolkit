using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.DesignScript.Geometry;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using BHoM.Structural;
using BHoM.Global;


namespace ETABSToolkit
{
    public class ExtractETABSNodes
    {
        public static void ExtractNodes(BHoM.Global.Project project,ETABS2013.cOAPI etabs, Boolean Activate = false)
        {
            NodeFactory nodes = project.Structure.Nodes;
               
            //Gets the ETABS geometry
            ETABS2013.cOAPI ETABS = etabs;
            int numberNames = 0;
            string[] pointList = null;
            ETABS.SapModel.PointObj.GetNameList(ref numberNames, ref pointList);

            List<BHoM.Geometry.Point> outPoints = new List<BHoM.Geometry.Point>();
            List<int> IDs = new List<int>();

            for (int i = 0; i < pointList.Count(); i++)
            {
                double x1 = 0;
                double y1 = 0;
                double z1 = 0;
                ETABS.SapModel.PointObj.GetCoordCartesian(pointList[i], ref x1, ref y1, ref z1);
                int name= Int32.Parse(pointList[i]);
                nodes.Create(name, x1, y1, z1);
            }
      
        }
    }
}
