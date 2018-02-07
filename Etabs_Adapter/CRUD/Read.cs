using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structural.Elements;
using ETABS2016;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        protected override IEnumerable<IObject> Read(Type type, IList ids)
        {
            if (type == typeof(Node))
                return ReadNodes(ids as dynamic);
            else if (type == typeof(Bar))
                return new List<Bar>(ids as dynamic);

            return null;//<--- returning null will throw error in replace method of BHOM_Adapter line 34: can't do typeof(null) - returning null does seem the most sensible to return though
        }

        private List<Node> ReadNodes(List<string> ids = null)
        {
            List<Node> nodeList = new List<Node>();

            nodeList = model.PointObj.ToBHoM(ids);

            return nodeList;
        }

    }
}
