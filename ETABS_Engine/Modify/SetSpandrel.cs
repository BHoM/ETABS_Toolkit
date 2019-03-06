using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;

namespace BH.Engine.ETABS
{
    public static partial class Modify
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static PanelPlanar SetSpandrel(this PanelPlanar panel, Spandrel spandrel)
        {
            PanelPlanar clone = (PanelPlanar)panel.GetShallowClone();

            clone.CustomData["EtabsSpandrel"] = spandrel;

            return clone;
        }

        /***************************************************/
    }
}