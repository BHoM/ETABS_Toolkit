using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;
using BH.Engine.Structure;
using BH.Engine.Geometry;

namespace BH.Engine.ETABS
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Spandrel Spandrel( string name)
        {
            return new Spandrel {  Name = name };
        }

        /***************************************************/
    }
}
