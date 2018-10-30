using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;

namespace BH.Engine.ETABS
{
    public static partial class Modify
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Bar SetInsertionPoint(this Bar bar, BarInsertionPoint barInsertionPoint = BarInsertionPoint.Centroid)
        {
            Bar clone = (Bar)bar.GetShallowClone();

            clone.CustomData["EtabsInsertionPoint"] = barInsertionPoint;

            return clone;
        }

        /***************************************************/
    }
}
