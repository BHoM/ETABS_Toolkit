using BHoM.Structural.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM.Base.Results;
using BHoM.Structural.Results;
using ETABS2015;

namespace Etabs_Adapter.Structural.Interface
{
    public partial class EtabsAdapter : IResultAdapter
    {
        public bool GetBarForces(List<string> bars, List<string> cases, int divisions, ResultOrder orderBy, out Dictionary<string, ResultSet<BarForce>> results)
        {
            throw new NotImplementedException();
        }

        public bool GetBarStresses()
        {
            throw new NotImplementedException();
        }

        public bool GetModalResults()
        {
            throw new NotImplementedException();
        }

        public bool GetNodeAccelerations(List<string> nodes, List<string> cases, ResultOrder orderBy, out Dictionary<string, ResultSet<NodeAcceleration>> results)
        {
            throw new NotImplementedException();
        }

        public bool GetNodeDisplacements(List<string> nodes, List<string> cases, ResultOrder orderBy, out Dictionary<string, ResultSet<NodeDisplacement>> results)
        {
            throw new NotImplementedException();
        }

        public bool GetNodeReactions(List<string> nodes, List<string> cases, ResultOrder orderBy, out Dictionary<string, ResultSet<NodeReaction>> results)
        {
            throw new NotImplementedException();
        }

        public bool GetNodeVelocities(List<string> nodes, List<string> cases, ResultOrder orderBy, out Dictionary<string, ResultSet<NodeVelocity>> results)
        {
            throw new NotImplementedException();
        }

        public bool GetPanelForces(List<string> panels, List<string> cases, ResultOrder orderBy, out Dictionary<string, ResultSet<PanelForce>> results)
        {
            throw new NotImplementedException();
        }

        public bool GetPanelStress(List<string> panels, List<string> cases, ResultOrder orderBy, out Dictionary<string, ResultSet<PanelStress>> results)
        {
            throw new NotImplementedException();
        }

        public bool StoreResults(string filename, List<ResultType> resultTypes, List<string> loadcases, bool append = false)
        {
            throw new NotImplementedException();
        }
    }
}
