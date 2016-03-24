using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Geometry;
using BHoM.Structural;
using BHoM.Global;


namespace ETABSToolkit
{
    public class CreateFrames
    {
        public static bool SolveInstance(object etabs, IEnumerable<BHoM.Structural.Bar> bars, bool activate = false)
        {
            Boolean trigger = false;
            ETABS2013.cOAPI ETABS = (ETABS2013.cOAPI) etabs;
            List<BHoM.Geometry.Point> outPoints = new List<BHoM.Geometry.Point>();
            List<int> IDs = new List<int>();
         
            if (activate)
            {
                foreach (BHoM.Structural.Bar bar in bars)
                {
                    string startnodename = "";
                    string endnodename = "";
                    string barname = "";
                    ETABS.SapModel.PointObj.AddCartesian( bar.StartNode.X,  bar.StartNode.Y,  bar.StartNode.Z, ref startnodename,"","Global",false,0);
                    ETABS.SapModel.PointObj.ChangeName(startnodename, bar.StartNode.Number.ToString());
                    ETABS.SapModel.PointObj.AddCartesian(bar.EndNode.X, bar.EndNode.Y, bar.EndNode.Z, ref startnodename, "", "Global", false, 0);
                    ETABS.SapModel.PointObj.ChangeName(endnodename, bar.EndNode.Number.ToString());
                    ETABS.SapModel.FrameObj.AddByPoint(bar.StartNode.Name, bar.EndNode.Name, ref barname, "Default", "");
                    ETABS.SapModel.FrameObj.ChangeName(barname, bar.Number.ToString());
                    
                }
                
            }
            return trigger;
        }
    }
}
