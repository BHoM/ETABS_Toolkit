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
        public static Node ToBHoM(this cPointObj point)
        {
            
            string name = "noPoint"; //wtf? there looks to be no way fo getting the coordinates withouot the name/id of the node!!!
            double x = 0;
            double y = 0;
            double z = 0;
            point.GetCoordCartesian(name, ref x, ref y, ref z);

            Node bhNode = new Node();
            bhNode.Name = name;
            bhNode.Position = new oM.Geometry.Point { X = x, Y = y, Z = z };

            //add conversion for other properties !!!

            return bhNode;
        }
         

        //public static Bar ToBHoM(this cFrameObj bar)
        //{


        //}
    }
}
