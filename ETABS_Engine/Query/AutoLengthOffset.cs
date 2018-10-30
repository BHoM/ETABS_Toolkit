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

        public static bool AutoLengthOffset(this Bar bar)
        {
            object obj;

            if (bar.CustomData.TryGetValue("EtabsAutoLengthOffset", out obj) && obj is bool)
            {
                return (bool)obj;
            }
            return false;
        }

        /***************************************************/

    }
}
