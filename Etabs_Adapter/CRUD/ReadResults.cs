using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Common;
using BH.oM.Structural.Results;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {

        protected override IEnumerable<IResult> ReadResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<IResult> results = new List<IResult>();

            if (typeof(StructuralGlobalResult).IsAssignableFrom(type))
                results = GetGlobalResults(type, cases);
            else
                results = GetObjectResults(type, ids, cases, divisions);

            return results;


        }

        private IEnumerable<IResult> GetGlobalResults(Type type, IList cases)
        {
            if (typeof(GlobalReactions).IsAssignableFrom(type))
                return GetGlobalReactions(cases);
            if (typeof(ModalDynamics).IsAssignableFrom(type))
                throw new NotImplementedException("modal dynamics not supported yet");

            return new List<IResult>();

        }

        private IEnumerable<IResult> GetGlobalReactions(IList cases)
        {


            return new List<IResult>();

        }

        private IEnumerable<IResult> GetObjectResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<IResult> results = new List<IResult>();

            if (typeof(NodeResult).IsAssignableFrom(type))
                results = GetNodeResults(type, ids, cases);
            else if (typeof(BarResult).IsAssignableFrom(type))
                results = GetBarResults(type, ids, cases, divisions);
            else if (typeof(PanelResult).IsAssignableFrom(type))
                results = GetPanelResults(type, ids, cases, divisions);
            //else
            //    return new List<IResult>();

            return results;
        }

        private IEnumerable<IResult> GetNodeResults(Type type, IList ids = null, IList cases = null)
        {
            IEnumerable<IResult> results = new List<NodeResult>();

            if (type == typeof(NodeAcceleration))
                results = Helper.GetNodeAcceleration(model, ids, cases);
            else if (type == typeof(NodeDisplacement))
                results = Helper.GetNodeDisplacement(model, ids, cases);
            else if (type == typeof(NodeReaction))
                results = Helper.GetNodeReaction(model, ids, cases);
            else if (type == typeof(NodeVelocity))
                results = Helper.GetNodeVelocity(model, ids, cases);

            return results;
        }

        private IEnumerable<IResult> GetBarResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<BarResult> results = new List<BarResult>();

            if (type == typeof(BarDeformation))
                results = Helper.GetBarDeformation(model, ids, cases, divisions);
            else if (type == typeof(BarForce))
                results = Helper.GetBarForce(model, ids, cases, divisions);
            else if (type == typeof(BarStrain))
                results = Helper.GetBarStrain(model, ids, cases, divisions);
            else if (type == typeof(BarStress))
                results = Helper.GetBarStress(model, ids, cases, divisions);

            return results;
        }

        private IEnumerable<IResult> GetPanelResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            IEnumerable<PanelResult> results = new List<PanelResult>();

            if (type == typeof(PanelForce))
                results = Helper.GetPanelForce(model, ids, cases, divisions);
            else if (type == typeof(PanelStress))
                results = Helper.GetPanelStress(model, ids, cases, divisions);

            return results;
        }

    }
}
