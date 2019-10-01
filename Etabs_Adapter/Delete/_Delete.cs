using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Adapter.ETABS
{
#if Debug18 || Release18
    public partial class ETABS18Adapter : BHoMAdapter
#elif Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        protected override int Delete(Type type, IEnumerable<object> ids)
        {
            return 0;
            throw new NotImplementedException();
        }

        /***************************************************/
    }
}
