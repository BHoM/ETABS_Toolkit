using BH.oM.Common.Materials;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Properties;
using BH.oM.Structure.Loads;
using System;
using System.Collections.Generic;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter : BHoMAdapter
    {
        /***************************************************/
        /**** BHoM Adapter Interface                    ****/
        /***************************************************/

        protected override List<Type> DependencyTypes<T>()
        {
            Type type = typeof(T);

            if (m_DependencyTypes.ContainsKey(type))
                return m_DependencyTypes[type];

            else if (m_DependencyTypes.ContainsKey(type.BaseType))
                return m_DependencyTypes[type.BaseType];

            else
            {
                foreach (Type interType in type.GetInterfaces())
                {
                    if (m_DependencyTypes.ContainsKey(interType))
                        return m_DependencyTypes[interType];
                }
            }


            return new List<Type>();
        }


        /***************************************************/
        /**** Private Fields                            ****/
        /***************************************************/

        private static Dictionary<Type, List<Type>> m_DependencyTypes = new Dictionary<Type, List<Type>>
        {
            {typeof(Bar), new List<Type> { typeof(ISectionProperty), typeof(Node) } },
            {typeof(ISectionProperty), new List<Type> { typeof(Material) } },
            {typeof(PanelPlanar), new List<Type> { typeof(IProperty2D), typeof(Material) } },
            {typeof(IProperty2D), new List<Type> { typeof(Material) } },
            {typeof(RigidLink), new List<Type> { typeof(Node) } },
            {typeof(ILoad), new List<Type> {typeof(Loadcase) } }

        };


        /***************************************************/
    }

}
