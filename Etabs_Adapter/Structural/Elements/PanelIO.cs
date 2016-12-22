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
    public class PanelIO
    {
        public static bool SetPanels(cOAPI Etabs, List<Panel> panels, out List<string> ids, string option = "")
        {
            cSapModel SapModel = Etabs.SapModel;
            ids = new List<string>();
            string name = "";
            Dictionary<string, string> addedThicknesses = new Dictionary<string, string>();

            for (int i = 0; i < panels.Count; i++)
            {
                Panel panel = panels[i];

                int edgeCount = panel.External_Contours.Count;
                List<Curve> c = Curve.Join(panel.External_Contours);
                string currentThickness = "";
                try
                {
                    if (c != null)
                    {
                        if (panel.PanelProperty != null && !addedThicknesses.TryGetValue(panel.PanelProperty.Name, out currentThickness))
                        {
                            PropertyIO.CreatePanelProperty(SapModel, panel.PanelProperty, panel.Material);
                            currentThickness = panel.PanelProperty.Name;
                            addedThicknesses.Add(currentThickness, currentThickness);
                        }

                        c.AddRange(panel.Internal_Contours);
                        for (int j = 0; j < c.Count; j++)
                        {
                            List<Point> segments = c[j].ControlPoints.ToList();

                            segments = Point.RemoveDuplicates(segments, 3);

                            double[] x = new double[segments.Count];
                            double[] y = new double[segments.Count];
                            double[] z = new double[segments.Count];

                            for (int k = 0; k < segments.Count; k++)
                            {
                                x[k] = segments[k].X;
                                y[k] = segments[k].Y;
                                z[k] = segments[k].Z;
                            }
                            
                            SapModel.AreaObj.AddByCoord(segments.Count, ref x, ref y, ref z, ref name, currentThickness);
                            if (j >= edgeCount)
                            {
                                SapModel.AreaObj.SetOpening(name, true);
                            }

                            ids.Add(name);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }

            return true;
        }

        public static bool SetOpenings(cOAPI Etabs, List<Opening> panels, out List<string> ids, string option = "")
        {
            cSapModel SapModel = Etabs.SapModel;
            ids = new List<string>();
            string name = "";
            Dictionary<string, string> addedThicknesses = new Dictionary<string, string>();

            for (int i = 0; i < panels.Count; i++)
            {
                Opening panel = panels[i];
                List<Curve> edges = Curve.Join(panel.Edges);
                
                for (int j = 0; j < edges.Count; j++)
                {
                    List<Point> segments = edges[j].ControlPoints;

                    segments = Point.RemoveDuplicates(segments, 3);

                    double[] x = new double[segments.Count];
                    double[] y = new double[segments.Count];
                    double[] z = new double[segments.Count];

                    for (int k = 0; k < segments.Count; k++)
                    {
                        x[k] = segments[k].X;
                        y[k] = segments[k].Y;
                        z[k] = segments[k].Z;
                    }

                    SapModel.AreaObj.AddByCoord(segments.Count, ref x, ref y, ref z, ref name, "");
                    SapModel.AreaObj.SetOpening(name, true);
                    ids.Add(name);
                }
            }

            return true;
        }

        public static Curve GetPerimeter(cOAPI Etabs, string areaName)
        {
            string[] pName = null;
            int pointCount = 0;
            double pX1 = 0;
            double pY1 = 0;
            double pZ1 = 0;

            Etabs.SapModel.AreaObj.GetPoints(areaName, ref pointCount, ref pName);

            List<Point> pts = new List<Point>();
            for (int j = 0; j < pointCount; j++)
            {
                Etabs.SapModel.PointObj.GetCoordCartesian(pName[j], ref pX1, ref pY1, ref pZ1);
                pts.Add(new Point(pX1, pY1, pZ1));
            }

            pts.Add(pts[0]);

            Polyline pl = new Polyline(pts);
            return pl;
        }

        public static List<string> GetPanels(cOAPI Etabs, out List<Panel> panels, ObjectSelection selection, List<string> ids = null)
        {
            cSapModel SapModel = Etabs.SapModel;
            List<string> outIds = new List<string>();

            Dictionary<string, PanelProperty> addedProperties = new Dictionary<string, PanelProperty>();
            Dictionary<string, Material> addedMaterials = new Dictionary<string, Material>();
            ObjectManager<string, Panel> panelManager = new ObjectManager<string, Panel>(EtabsUtils.NUM_KEY, FilterOption.UserData);

            int numberArea = 0;
            bool selected = false;
            string[] names = null;


            if (selection == ObjectSelection.FromInput)
            {
                numberArea = ids.Count;
                names = ids.ToArray();
            }
            else
            {
                SapModel.AreaObj.GetNameList(ref numberArea, ref names);
            }

            for (int i = 0; i < numberArea; i++)
            {
                if (selection == ObjectSelection.Selected)
                {
                    SapModel.AreaObj.GetSelected(names[i], ref selected);
                    if (!selected) continue;
                }

                outIds.Add(names[i]);              
                
                PanelProperty panelProp = null;
                Material materialProp = null;
                string propertyName = "";
                string material = "";

                SapModel.AreaObj.GetProperty(names[i], ref propertyName);

                if (!addedProperties.TryGetValue(propertyName, out panelProp))
                {
                    panelProp = PropertyIO.GetPanelProperty(SapModel, propertyName, out material);
                    addedProperties.Add(propertyName, panelProp);
                    addedMaterials.Add(propertyName, EtabsUtils.GetMaterial(SapModel, material));
                }
                addedMaterials.TryGetValue(propertyName, out materialProp);

                Panel p = panelManager.Add(names[i], new Panel());

                p.PanelProperty = panelProp;
                if (p.PanelProperty != null ) p.PanelProperty.Material = materialProp;
                p.External_Contours = new BHoM.Geometry.Group<Curve>() { GetPerimeter(Etabs, names[i]) };
            }

            panels = panelManager.GetRange(outIds);
            return outIds;
        }

        public static List<string> GetOpenings(cOAPI Etabs, out List<Opening> panels, ObjectSelection selection, List<string> ids = null)
        {
            cSapModel SapModel = Etabs.SapModel;
            List<string> outIds = new List<string>();
            ObjectManager<string, Opening> openingManager = new ObjectManager<string, Opening>(EtabsUtils.NUM_KEY, FilterOption.UserData);

            int numberArea = 0;
            bool selected = false;
            string[] names = null;
            bool isOpening = false;

            if (selection == ObjectSelection.FromInput)
            {
                numberArea = ids.Count;
                names = ids.ToArray();
            }
            else
            {
                SapModel.AreaObj.GetNameList(ref numberArea, ref names);
            }

            for (int i = 0; i < numberArea; i++)
            {
                if (selection == ObjectSelection.Selected)
                {
                    SapModel.AreaObj.GetSelected(names[i], ref selected);
                    if (!selected) continue;
                }
                SapModel.AreaObj.GetOpening(names[i], ref isOpening);
                if (isOpening)
                {
                    outIds.Add(names[i]);
                    Opening p = openingManager.Add(names[i], new Opening());   
                    p.Edges = new BHoM.Geometry.Group<Curve>() { GetPerimeter(Etabs, names[i]) };
                }
            }

            panels = openingManager.GetRange(outIds);
            return outIds;
        }
    }
}
