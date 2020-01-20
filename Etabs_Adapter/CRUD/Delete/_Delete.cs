using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Adapter;

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        protected override int IDelete(Type type, IEnumerable<object> ids, ActionConfig actionConfig = null)
        {
            return base.IDelete(type, ids, actionConfig);
        }

        /***************************************************/
    }
}
