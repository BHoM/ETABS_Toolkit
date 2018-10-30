using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structure.Loads;

namespace BH.oM.Structure.Loads
{
    public class MassSource : BHoMObject
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        public bool ElementSelfMass { get; set; } = true;

        public bool AdditionalMass { get; set; } = true;

        public List<Tuple<Loadcase, double>> FactoredAdditionalCases { get; set; } = new List<Tuple<Loadcase, double>>();



        /***************************************************/
    }
}
