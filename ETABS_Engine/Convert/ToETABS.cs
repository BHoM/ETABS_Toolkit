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

namespace BH.Adapter.ETABS
{
    public static partial class Convert
    {
        public static cPointObj ToETABS(this Node bhNode, cSapModel model)
        {
            cPointObj etabsNode;
            string name = bhNode.Name;
            etabsNode.AddCartesian(bhNode.Position.X, bhNode.Position.Y, bhNode.Position.Z, ref name);
        }

        public static cFrameObj ToETABS(this Bar bhBar, cSapModel model)
        {

        }
    }
}
