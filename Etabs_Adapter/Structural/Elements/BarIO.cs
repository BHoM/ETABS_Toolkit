using BHoM.Base;
using BHoM.Geometry;
using BHoM.Materials;
using BHoM.Structural.Elements;
using BHoM.Structural.Interface;
using BHoM.Structural.Properties;
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

        public static BarRelease GetBarRelease(cOAPI Etabs, string barId)
        {
            cSapModel SapModel = Etabs.SapModel;
            bool[] ii = null;
            bool[] jj = null;
            double[] s = null;
            double[] e = null;

            if (SapModel.FrameObj.GetReleases(barId, ref ii, ref jj, ref s, ref e) == 0)
            {
                return new BarRelease(new NodeConstraint("", ii, s), new NodeConstraint("", jj, e));
            }
            return null;
        }

        public static List<string> GetBars(cOAPI Etabs, out List<Bar> bars, ObjectSelection selection, List<string> ids = null)
        {
            cSapModel SapModel = Etabs.SapModel;
            ObjectManager<string, Bar> barManager = new ObjectManager<string, Bar>(EtabsUtils.NUM_KEY, FilterOption.UserData);
            ObjectManager<string, Node> nodeManager = new ObjectManager<string, Node>(EtabsUtils.NUM_KEY, FilterOption.UserData);
            ObjectManager<BarRelease> addedReleases = new ObjectManager<BarRelease>();
            Dictionary<string, SectionProperty> loadedProperties = new Dictionary<string, SectionProperty>();
            Dictionary<string, Material> addedMaterials = new Dictionary<string, Material>();
            List<string> outIds = new List<string>();

            int numberFrames = 0;
            bool selected = false;
            string[] names = null;
            string[] property = null;
            string[] story = null;
            string[] p1 = null;
            string[] p2 = null;
            double[] pX1 = null;
            double[] pX2 = null;
            double[] pY1 = null;
            double[] pY2 = null;
            double[] pZ1 = null;
            double[] pZ2 = null;
            double[] angle = null;
            double[] oX1 = null;
            double[] oX2 = null;
            double[] oY1 = null;
            double[] oY2 = null;
            double[] oZ1 = null;
            double[] oZ2 = null;
            int[] cardinalPoint = null;
            string nameExists = "";

            SapModel.FrameObj.GetAllFrames(ref numberFrames, ref names, ref property, ref story, ref p1, ref p2, ref pX1, ref pY1, ref pZ1, ref pX2,
                    ref pY2, ref pZ2, ref angle, ref oX1, ref oX2, ref oY1, ref oY2, ref oZ1, ref oZ2, ref cardinalPoint);

            Dictionary<string, string> barSelection = new Dictionary<string, string>();

            if (selection == ObjectSelection.FromInput)
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    barSelection.Add(ids[i], ids[i]);
                }
            }

            for (int i = 0; i < numberFrames; i++)
            {
                if (selection == ObjectSelection.Selected)
                {
                    SapModel.FrameObj.GetSelected(names[i], ref selected);
                    if (!selected) continue;
                }

                if (selection == ObjectSelection.FromInput && !barSelection.TryGetValue(names[i], out nameExists)) continue;

                outIds.Add(names[i]);
                nodeManager.Add(p1[i], new Node(pX1[i], pY1[i], pZ1[i]));
                nodeManager.Add(p2[i], new Node(pX2[i], pY2[i], pZ2[i]));

                Bar bar = barManager.Add(names[i], new Bar(nodeManager[p1[i]], nodeManager[p2[i]]));

                SectionProperty barProp = null;
                string material = "";
                if (!loadedProperties.TryGetValue(property[i], out barProp))
                {
                    barProp = PropertyIO.GetBarProperty(SapModel, property[i], bar.Line.Direction.IsParallel(Vector.ZAxis(), Math.PI/12), out material);
                    loadedProperties.Add(property[i], barProp);
                    addedMaterials.Add(property[i], EtabsUtils.GetMaterial(SapModel, material));
                }

                Material matProp = null;
                addedMaterials.TryGetValue(property[i], out matProp);

                BarRelease release = GetBarRelease(Etabs, names[i]);
                if (release != null)
                {
                    bar.Release = addedReleases.Add(release.Name, release);
                }
               
                bar.SectionProperty = barProp;
                bar.Material = matProp;
                bar.OrientationAngle = angle[i];
                

                barManager.Add(EtabsUtils.NUM_KEY, bar);
            }
            bars = barManager.GetRange(outIds);
            return outIds;
        }
    }
}
