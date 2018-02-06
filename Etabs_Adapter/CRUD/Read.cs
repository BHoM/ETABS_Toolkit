using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structural.Elements;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        protected override IEnumerable<IObject> Read(Type type, IList ids)
        {
            if (type == typeof(Node))
                return new List<Node>();//Call something like: private List<Node> ReadNodes(List<string> ids)
            else if (type == typeof(Bar))
                return new List<Bar>();

            return null;//<--- returning null will throw error in replace method of BHOM_Adapter line 34: can't do typeof(null) - returning null does seem the most sensible to return though
        }

        private List<Node> ReadNodes(List<int> ids = null)
        {
            List<Node> nodeList = new List<Node>();
            if (ids == null)
            {
                //get all nodes
            }
            else
            {
                //get nodes by id
            }
            return nodeList;
        }

    }
}
