﻿using BHoM.Base;
using BHoM.Geometry;
using BHoM.Materials;
using BHoM.Structural.Elements;
using BHoM.Structural.Interface;
using BHoM.Structural.Properties;
using Etabs_Adapter.Base;
using Etabs_Adapter.Structural.Properties;
using ETABS2016;
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
            string currentSection = null;
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
                
                double angleMutliplier = n1.Z < n2.Z ? 1 : n1.Z == n2.Z && n1.X < n2.X ? 1 : n1.Z == n2.Z && n1.X == n2.X && n1.Y < n2.Y ? 1 : -1;
                double verticalAdjustment = 0;


                if (Utils.NearEqual(n1.X, n2.X, 0.0001) && Utils.NearEqual(n1.Y, n2.Y, 0.0001)) // Vertical rotate by 90
                {
                    verticalAdjustment = 90;
                }

                SapModel.FrameObj.SetLocalAxes(name, angleMutliplier * bars[i].OrientationAngle * 180 / Math.PI + verticalAdjustment);

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

                    startVals[0] = (bar.Release.StartConstraint.UX == DOFType.Fixed) ? 1 : (bar.Release.StartConstraint.UX == DOFType.Free) ? 0 : startVals[0];
                    startVals[1] = (bar.Release.StartConstraint.UY == DOFType.Fixed) ? 1 : (bar.Release.StartConstraint.UY == DOFType.Free) ? 0 : startVals[1];
                    startVals[2] = (bar.Release.StartConstraint.UZ == DOFType.Fixed) ? 1 : (bar.Release.StartConstraint.UZ == DOFType.Free) ? 0 : startVals[2];
                    startVals[3] = (bar.Release.StartConstraint.RX == DOFType.Fixed) ? 1 : (bar.Release.StartConstraint.RX == DOFType.Free) ? 0 : startVals[3];
                    startVals[4] = (bar.Release.StartConstraint.RY == DOFType.Fixed) ? 1 : (bar.Release.StartConstraint.RY == DOFType.Free) ? 0 : startVals[4];
                    startVals[5] = (bar.Release.StartConstraint.RZ == DOFType.Fixed) ? 1 : (bar.Release.StartConstraint.RZ == DOFType.Free) ? 0 : startVals[5];

                    endVals[0] = (bar.Release.EndConstraint.UX == DOFType.Fixed) ? 1 : (bar.Release.EndConstraint.UX == DOFType.Free) ? 0 : endVals[0];
                    endVals[1] = (bar.Release.EndConstraint.UY == DOFType.Fixed) ? 1 : (bar.Release.EndConstraint.UY == DOFType.Free) ? 0 : endVals[1];
                    endVals[2] = (bar.Release.EndConstraint.UZ == DOFType.Fixed) ? 1 : (bar.Release.EndConstraint.UZ == DOFType.Free) ? 0 : endVals[2];
                    endVals[3] = (bar.Release.EndConstraint.RX == DOFType.Fixed) ? 1 : (bar.Release.EndConstraint.RX == DOFType.Free) ? 0 : endVals[3];
                    endVals[4] = (bar.Release.EndConstraint.RY == DOFType.Fixed) ? 1 : (bar.Release.EndConstraint.RY == DOFType.Free) ? 0 : endVals[4];
                    endVals[5] = (bar.Release.EndConstraint.RZ == DOFType.Fixed) ? 1 : (bar.Release.EndConstraint.RZ == DOFType.Free) ? 0 : endVals[5];
                    
                    SapModel.FrameObj.SetReleases(name, ref ii, ref jj, ref startVals, ref endVals);
                }

                if (bar.Offset != null)
                {
                    //get something going in here.
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

                Bar bar = barManager.Add(names[i], new Bar());

                bar.StartNode = nodeManager[p1[i]];
                bar.EndNode = nodeManager[p2[i]];

                SectionProperty barProp = null;
                string material = "";
                if (!loadedProperties.TryGetValue(property[i], out barProp) && !string.IsNullOrEmpty(property[i]))
                {               
                    barProp = PropertyIO.GetBarProperty(SapModel, property[i], bar.Line.Direction.IsParallel(Vector.ZAxis(), Math.PI/12), out material);
                    loadedProperties.Add(property[i], barProp);
                    barProp.Material = EtabsUtils.GetMaterial(SapModel, material);
                }

                BarRelease release = GetBarRelease(Etabs, names[i]);
                if (release != null)
                {
                    bar.Release = addedReleases.Add(release.Name, release);
                }
               
                bar.SectionProperty = barProp;
                bar.OrientationAngle = -angle[i] * Math.PI / 180;
                
            }
            bars = barManager.GetRange(outIds);
            return outIds;
        }
    }
}