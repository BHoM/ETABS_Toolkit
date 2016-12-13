using BHoM.Structural.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM.Base.Results;
using BHoM.Structural.Results;
using ETABS2015;
using Etabs_Adapter.Structural.Results;

namespace Etabs_Adapter.Structural.Interface
{
    public partial class EtabsAdapter : IResultAdapter
    {
        public bool GetBarForces(List<string> bars, List<string> cases, int divisions, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            ResultServer<BarForce<string, string, string>> resultServer = new ResultServer<BarForce<string, string, string>>();
            resultServer.OrderBy = orderBy;
            BarResults.GetBarForces(Etabs, resultServer, bars, cases, divisions);
            results = resultServer.LoadData();
            return true;
        }

        public bool GetBarStresses()
        {
            throw new NotImplementedException();
        }

        public bool GetBarUtilisation(List<string> bars, List<string> cases, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            throw new NotImplementedException();
        }

        public bool GetModalResults()
        {
            throw new NotImplementedException();
        }

        public bool GetNodeAccelerations(List<string> nodes, List<string> cases, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            throw new NotImplementedException();
        }

        public bool GetNodeCoordinates(List<string> nodes, out Dictionary<string, IResultSet> results)
        {
            throw new NotImplementedException();
        }

        public bool GetNodeDisplacements(List<string> nodes, List<string> cases, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            ResultServer<NodeDisplacement<string, string, string>> resultServer = new ResultServer<NodeDisplacement<string, string, string>>();
            resultServer.OrderBy = orderBy;
            NodeResults.GetNodeDisplacements(Etabs, resultServer, nodes, cases);
            results = resultServer.LoadData();
            return true;
        }

        public bool GetNodeReactions(List<string> nodes, List<string> cases, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            ResultServer<NodeReaction<string, string, string>> resultServer = new ResultServer<NodeReaction<string, string, string>>();
            resultServer.OrderBy = orderBy;
            NodeResults.GetNodeReactions(Etabs, resultServer, nodes, cases);
            results = resultServer.LoadData();
            return true;
        }

        public bool GetNodeVelocities(List<string> nodes, List<string> cases, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            throw new NotImplementedException();
        }

        public bool GetPanelForces(List<string> panels, List<string> cases, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            throw new NotImplementedException();
        }

        public bool GetPanelStress(List<string> panels, List<string> cases, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            throw new NotImplementedException();
        }

        public bool GetSlabReinforcement(List<string> panels, List<string> cases, ResultOrder orderBy, out Dictionary<string, IResultSet> results)
        {
            throw new NotImplementedException();
        }

        public bool StoreResults(string filename, List<ResultType> resultTypes, List<string> loadcases, bool append = false)
        {
            foreach (ResultType t in resultTypes)
            {
                switch (t)
                {
                    case ResultType.BarForce:
                        BarResults.GetBarForces(Etabs, new ResultServer<BarForce<string, string, string>>(filename, append), null, loadcases, 3);
                        break;
                    case ResultType.BarStress:
                        // BarResults.GetBarStress(Robot, new BHoMBR.ResultServer<BHoMR.BarStress>(filename), null, loadcases, 3);
                        break;
                    case ResultType.NodeReaction:
                        NodeResults.GetNodeReactions(Etabs, new ResultServer<NodeReaction<string, string, string>>(filename, append), null, loadcases);
                        break;
                    case ResultType.NodeDisplacement:
                        NodeResults.GetNodeDisplacements(Etabs, new ResultServer<NodeDisplacement<string, string, string>>(filename, append), null, loadcases);
                        break;
                    case ResultType.PanelForce:
                        PanelResults.GetPanelForces(Etabs, new ResultServer<PanelForce<string, string, string>>(filename, append), null, loadcases);
                        break;
                    case ResultType.PanelStress:
                        //PanelResults.GetPanelStress(Robot, new BHoMBR.ResultServer<BHoMR.PanelStress>(filename), null, loadcases);
                        break;

                }
            }
            return true;
        }
    }
}
