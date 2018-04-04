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
            if (typeof(StructuralGlobalResult).IsAssignableFrom(type))
                return GetGlobalResults(type, cases);
            else
                return GetObjectResults(type, ids, cases, divisions);

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
            if (type == typeof(NodeResult))
                return GetNodeResults(type, ids, cases, divisions);
            else if (type == typeof(BarResult))
                return GetBarResults(type, ids, cases, divisions);
            else if (type == typeof(PanelResult))
                return GetPanelResults(type, ids, cases, divisions);
            else
                return new List<IResult>();
        }

        private List<IResult> GetNodeResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            List<IResult> results = new List<IResult>();

            if (type == typeof(NodeAcceleration))
                results = Helper.GetNodeAcceleration(model, ids, cases, divisions);
            else if (type == typeof(NodeDisplacement))
                results = Helper.GetNodeDisplacement(model, ids, cases, divisions);
            else if (type == typeof(NodeReaction))
                results = Helper.GetNodeReaction(model, ids, cases, divisions);
            else if (type == typeof(NodeVelocity))
                results = Helper.GetNodeVelocity(model, ids, cases, divisions);

            return results();
        }

        private List<IResult> GetBarResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            if (type == typeof(BarDeformation))
                return Helper.GetBarDeformation(model, ids, cases, divisions);
            else if (type == typeof(BarForce))
                return Helper.GetBarForce(model, ids, cases, divisions);
            else if (type == typeof(BarStrain))
                return Helper.GetBarStrain(model, ids, cases, divisions);
            else if (type == typeof(BarStress))
                return Helper.GetBarStress(model, ids, cases, divisions);


            return new List<IResult>();
        }

        private List<IResult> GetPanelResults(Type type, IList ids = null, IList cases = null, int divisions = 5)
        {
            if (type == typeof(PanelForce))
                return Helper.GetPanelForce(model, ids, cases, divisions);
            else if (type == typeof(PanelStress))
                return Helper.GetPanelStress(model, ids, cases, divisions);

            return new List<IResult>();
        }

    }
}
