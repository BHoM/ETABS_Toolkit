using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structural.Elements;
using BH.oM.Structural.Properties;
using BH.oM.Common.Materials;
using BH.Engine.Base.Objects;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter : BHoMAdapter
    {

        /***************************************************/
        /**** BHoM Adapter Interface                    ****/
        /***************************************************/

        protected override IEqualityComparer<T> Comparer<T>()
        {
            Type type = typeof(T);

            if (m_Comparers.ContainsKey(type))
            {
                return m_Comparers[type] as IEqualityComparer<T>;
            }
            else
            {
                return EqualityComparer<T>.Default;
            }

        }


        /***************************************************/
        /**** Private Fields                            ****/
        /***************************************************/

        private static Dictionary<Type, object> m_Comparers = new Dictionary<Type, object>
        {
            {typeof(Node), new BH.Engine.Structure.NodeDistanceComparer(3) },
            {typeof(ISectionProperty), new BHoMObjectNameOrToStringComparer() },
            {typeof(Material), new BHoMObjectNameComparer() },
        };


        /***************************************************/
    }
}
