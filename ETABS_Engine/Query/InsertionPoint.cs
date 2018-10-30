using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;

namespace BH.Engine.ETABS
{
    public static partial class Query
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static BarInsertionPoint InsertionPoint(this Bar bar)
        {
            object obj;

            if (bar.CustomData.TryGetValue("EtabsInsertionPoint", out obj) && obj is BarInsertionPoint)
            {
                return (BarInsertionPoint)obj;
            }
            return BarInsertionPoint.Centroid;
        }

        /***************************************************/

    }
}
