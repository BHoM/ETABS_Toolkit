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
        public static bool SolveInstance(object etabs, IEnumerable<BHoM.Structural.Bar> bars,bool delete = false, bool activate = false)
        {
            Boolean trigger = false;
            ETABS2015.cOAPI ETABS = (ETABS2015.cOAPI) etabs;
            List<BHoM.Geometry.Point> outPoints = new List<BHoM.Geometry.Point>();
            List<int> IDs = new List<int>();
         
            if (activate && bars.Count()!=0)
            {
                string startnodename = "";
                string endnodename = "";
                string barname = "";
                string sectionname = "";
                string material = "";
                int NumberNames = 0;
                string[] ETABSexistingsections = new string[300];
                string[] ETABSexistingmaterials = new string[300];
                string[] ETABSexistingframes = new string[300];
                int ret = 5;

                ret = ETABS.SapModel.PropFrame.GetNameList(ref NumberNames, ref ETABSexistingsections, 0);
                ret = ETABS.SapModel.PropMaterial.GetNameList(ref NumberNames, ref ETABSexistingmaterials, 0);
                ret = ETABS.SapModel.FrameObj.GetNameList(ref NumberNames, ref ETABSexistingframes);
                ////Delete frames in current Model
                //if (delete && (ETABSexistingframes[0] != null))
                //{
                //    foreach (string frame in ETABSexistingframes)
                //    {
                //        ret = ETABS.SapModel.FrameObj.Delete(frame, ETABS2015.eItemType.Objects);
                //    }
                //}
                foreach (BHoM.Structural.Bar bar in bars)
                {
             //DETERMINE IF THE SECTIONS AND MATERIAL OF THE BHOM SECTION EXIST IN THE MODEL IF NOT CREATE

                  if (!ETABSexistingmaterials.Contains(bar.SectionProperty.Material.Name.ToString()))
                  { 
                  ret = ETABS.SapModel.PropMaterial.AddMaterial(ref material, ETABS2015.eMatType.Steel, "United States", "Steel", "ASTM A992", "");
                  ret = ETABS.SapModel.PropMaterial.ChangeName(material, bar.SectionProperty.Material.Name.ToString());
                  }
                  if (!ETABSexistingsections.Contains(bar.SectionProperty.Name.ToString()))
                  { ret = ETABS.SapModel.PropFrame.SetTube(bar.SectionProperty.Name.ToString(), bar.SectionProperty.Material.Name.ToString(), bar.SectionProperty.Depth / 2, bar.SectionProperty.Width / 2, bar.SectionProperty.TopFlangeThickness, bar.SectionProperty.WebThickness, -1, "", ""); }
                 

                    //Insert frame

                      ret = ETABS.SapModel.PointObj.AddCartesian(bar.StartNode.X, bar.StartNode.Y, bar.StartNode.Z, ref startnodename, "", "Global", false, 0);
                      ret = ETABS.SapModel.PointObj.ChangeName(startnodename, bar.StartNode.Number.ToString());
                      ret = ETABS.SapModel.PointObj.AddCartesian(bar.EndNode.X, bar.EndNode.Y, bar.EndNode.Z, ref endnodename, "", "Global", false, 0);
                      ret = ETABS.SapModel.PointObj.ChangeName(endnodename, bar.EndNode.Number.ToString());
                      ret = ETABS.SapModel.FrameObj.AddByPoint(startnodename, endnodename, ref barname, bar.SectionProperty.Name.ToString(), "");
                      ret = ETABS.SapModel.FrameObj.ChangeName(barname, bar.Number.ToString());

                      bar.Level = "asdf";

                }
                ETABS.SapModel.View.RefreshView();
            }
            return trigger;
        }
    }
}
