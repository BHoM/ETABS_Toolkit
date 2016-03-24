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
    public class ExtractETABSFrames
    {
        //public static void ExtractETABSFrames(BHoM.Global.Project project, ETABS2013.cOAPI etabs, Boolean Activate = false)
        //{
        //    BarFactory bars = project.Structure.Bars;
               
        //    //Gets the ETABS geometry
        //    ETABS2013.cOAPI ETABS = etabs;
        //    int frameNames = 0;
        //    string[] frameList = null;
        //    ETABS.SapModel.FrameObj.GetNameList(ref frameNames, ref frameList);

        //    for (int i = 0; i < frameList.Count(); i++)
        //    {
        //        string point1 = null;
        //        string point2 = null;
        //        double x1 = 0;
        //        double x2 = 0;
        //        double y1 = 0;
        //        double y2 = 0;
        //        double z1 = 0;
        //        double z2 = 0;
        //        ETABS.SapModel.FrameObj.GetPoints(frameList[i], ref point1, ref point2);
        //        ETABS.SapModel.PointObj.GetCoordCartesian(point1, ref x1, ref y1, ref z1);
        //        ETABS.SapModel.PointObj.GetCoordCartesian(point2, ref x2, ref y2, ref z2);

        //    }
 
        //}
    }
}
