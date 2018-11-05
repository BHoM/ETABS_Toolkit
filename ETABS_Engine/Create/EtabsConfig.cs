using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Adapters.ETABS;


namespace BH.Engine.ETABS
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static EtabsConfig EtabsConfig(bool replaceLoads = false)
        {
            return new EtabsConfig
            {
                ReplaceLoads = replaceLoads
            };
        }

        /***************************************************/
    }
}
