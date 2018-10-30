using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structure.Elements;

namespace BH.oM.Adapters.ETABS.Elements
{
    public class Diaphragm : BHoMObject
    {
        /***************************************************/
        /**** Public Properties                         ****/
        /***************************************************/

        public DiaphragmType Rigidity { get; set; } = DiaphragmType.RigidDiaphragm;

        /***************************************************/
    }
}
