using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.oM.Adapters.ETABS
{
    public enum BarInsertionPoint
    {
        BottomLeft = 1,
        BottomCenter = 2,
        BottomRight = 3,
        MiddleLeft = 4,
        MiddleCenter = 5,
        MiddleRight = 6,
        TopLeft = 7,
        TopCenter = 8,
        TopRight = 9,
        Centroid = 10,
        ShearCenter = 11
    }
}
