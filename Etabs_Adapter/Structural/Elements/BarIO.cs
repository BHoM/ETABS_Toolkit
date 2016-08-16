using BHoM.Base;
using BHoM.Geometry;
using BHoM.Structural.Elements;
using Etabs_Adapter.Base;
using Etabs_Adapter.Structural.Properties;
using ETABS2015;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etabs_Adapter.Structural.Elements
{
    public class BarIO
    {
        public static bool SetBars(cOAPI Etabs, List<Bar> bars, out List<string> ids)
        {
            cSapModel SapModel = Etabs.SapModel;
            ids = new List<string>();
            string currentSection = "";
            Dictionary<string, string> addedSections = new Dictionary<string, string>();
            Dictionary<string, string> addedSprings = new Dictionary<string, string>();
            Dictionary<Guid, Node> addedNodes = new Dictionary<Guid, Node>();

            foreach (Bar bar in bars)
            {
                Node data = null;
                if (!addedNodes.TryGetValue(bar.StartNode.BHoM_Guid, out data))
                {
                    addedNodes.Add(bar.StartNode.BHoM_Guid, bar.StartNode);
                }
                if (!addedNodes.TryGetValue(bar.EndNode.BHoM_Guid, out data))
                {
                    addedNodes.Add(bar.EndNode.BHoM_Guid, bar.EndNode);
                }
            }

            List<string> nodeIds = new List<string>();
            NodeIO.CreateNodes(Etabs, addedNodes.Values.ToList(), out nodeIds);
            string name = "";
            for (int i = 0; i < bars.Count; i++)
            {
                Bar bar = bars[i];
                Point n1 = bars[i].StartNode.Point;
                Point n2 = bars[i].EndNode.Point;

                SapModel.FrameObj.AddByCoord(n1.X, n1.Y, n1.Z, n2.X, n2.Y, n2.Z, ref name);
                EtabsUtils.SetDefaultKeyData(bars[i].CustomData, name);

                ids.Add(name);

                SapModel.FrameObj.SetLocalAxes(name, bars[i].OrientationAngle);

                if (bar.SectionProperty != null && !addedSections.TryGetValue(bar.SectionProperty.Name, out currentSection))
                {
                    PropertyIO.CreateBarProperty(SapModel, bar.SectionProperty, bar.Material);
                    currentSection = bar.SectionProperty.Name;
                    addedSections.Add(currentSection, currentSection);
                }
                SapModel.FrameObj.SetSection(name, currentSection);
                if (bar.Release != null)
                {
                    bool[] ii = bar.Release.StartConstraint.Freedom();
                    bool[] jj = bar.Release.EndConstraint.Freedom();
                    double[] startVals = bar.Release.StartConstraint.ElasticValues();
                    double[] endVals = bar.Release.EndConstraint.ElasticValues();
                    SapModel.FrameObj.SetReleases(name, ref ii, ref jj, ref startVals, ref endVals);
                }
            }
            return true;
        }
    }
}
