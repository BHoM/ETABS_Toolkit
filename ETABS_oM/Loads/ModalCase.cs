using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;

namespace BH.oM.Structure.Loads
{
    public class ModalCase : BHoMObject, ICase
    {
        public int Number { get; set; } = 0;

        public int NumberOfModes { get; set; } = 20;

        public int StartMode { get; set; } = 1;

        public MassSource Mass { get; set; } = new MassSource();
        
    }
}
