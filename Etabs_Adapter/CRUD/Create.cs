using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        protected override bool Create<T>(IEnumerable<T> objects, bool replaceAll = false)
        {
            bool success = true;

            if (typeof(BH.oM.Base.IObject).IsAssignableFrom(typeof(T)))
            {
                foreach (T obj in objects)
                {
                    obj.ToETABS(model);
                }
            }
            else
            {
                success = false;
            }
            
            return success;
        }

    }
}
