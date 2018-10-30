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

        public static PanelPlanar SetDiaphragm(this PanelPlanar panel, Diaphragm diaphragm)
        {
            PanelPlanar clone = (PanelPlanar)panel.GetShallowClone();

            clone.CustomData["EtabsDiaphragm"] = diaphragm;

            return clone;
        }

        /***************************************************/
    }
}
