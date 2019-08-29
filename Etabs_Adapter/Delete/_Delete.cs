using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Adapter.SAP2000
{
    public partial class SAP2000Adapter
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
