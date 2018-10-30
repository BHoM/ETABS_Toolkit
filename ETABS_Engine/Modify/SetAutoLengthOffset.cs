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

        public static Bar SetAutoLengthOffset(this Bar bar, bool autoLengthOffset)
        {
            Bar clone = (Bar)bar.GetShallowClone();

            clone.CustomData["EtabsAutoLengthOffset"] = autoLengthOffset;

            return clone;
        }

        /***************************************************/
    }
}
