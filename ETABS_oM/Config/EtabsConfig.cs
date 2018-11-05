using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Reflection.Attributes;
using System.ComponentModel;

namespace BH.oM.Adapters.ETABS
{
    public class EtabsConfig : BHoMObject
    {
        /***************************************************/
        /**** Public Properties                         ****/
        /***************************************************/

        [Description("Sets whether the loads being pushed should overwrite existing loads on the same object within the same loadcase")]
        public bool ReplaceLoads { get; set; } = false;

        /***************************************************/
    }
}
