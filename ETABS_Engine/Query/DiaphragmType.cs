//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using BH.oM.Structure.Elements;
//using BH.oM.Adapters.ETABS;

//namespace BH.Engine.ETABS
//{
//    public static partial class Query
//    {

//        /***************************************************/
//        /**** Public Methods                            ****/
//        /***************************************************/

//        public static DiaphragmType? Diaphragm(this PanelPlanar panel)
//        {
//            object obj;

//            if (panel.CustomData.TryGetValue("EtabsDiaphragm", out obj) && obj is DiaphragmType)
//            {
//                return (DiaphragmType)obj;
//            }
//            return null;
//        }

//        /***************************************************/

//    }
//}
