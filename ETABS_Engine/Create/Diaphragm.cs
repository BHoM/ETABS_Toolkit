using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;
using BH.Engine.Structure;
using BH.Engine.Geometry;

namespace BH.Engine.ETABS
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Diaphragm Diaphragm(/*List<PanelPlanar> panels,*/ string name, DiaphragmType type = DiaphragmType.RigidDiaphragm)
        {
            //List<double> zvals = panels.SelectMany(x => x.AllEdgeCurves().SelectMany(y => y.IControlPoints().Select(z => z.Z))).ToList();

            //if (zvals.Where(x => Math.Abs(zvals.First() - x) > BH.oM.Geometry.Tolerance.Distance).Count() > 0)
            //{
            //    BH.Engine.Reflection.Compute.RecordError("All panels need to be in the same plane");
            //    return null;
            //}

            return new Diaphragm { /*Panels = panels,*/ Name = name, Rigidity = type };
        }

        /***************************************************/
    }
}
