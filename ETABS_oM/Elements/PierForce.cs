using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structure.Elements;

namespace BH.oM.Adapters.ETABS.Elements
{
    public class PierForce : BH.oM.Structure.Results.BarForce
    {
        //Just using this for the name
        public string Location { get; set; } = "";
    }
}
