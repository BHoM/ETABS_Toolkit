using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS.Elements;

namespace BH.Engine.ETABS
{
    public static partial class Query
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Spandrel Spandrel(this PanelPlanar panel)
        {
            object obj;

            if (panel.CustomData.TryGetValue("EtabsSpandrel", out obj) && obj is Spandrel)
            {
                return (Spandrel)obj;
            }
            return null;
        }

        /***************************************************/

    }
}