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
                BHoM.Geometry.Group<Curve> c = panel.External_Contours;
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

            string[] pName = null;
            double pX1 = 0;
            double pY1 = 0;
            double pZ1 = 0;

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

                int pointCount = 0;

                SapModel.AreaObj.GetPoints(names[i], ref pointCount, ref pName);

                List<Point> pts = new List<Point>();
                for (int j = 0; j < pointCount; j++)
                {
                    SapModel.PointObj.GetCoordCartesian(pName[j], ref pX1, ref pY1, ref pZ1);
                    pts.Add(new Point(pX1, pY1, pZ1));
                }

                pts.Add(pts[0]);

                Polyline pl = new Polyline(pts);
                
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

                Panel p = new Panel(new BHoM.Geometry.Group<Curve>() { pl });
                p.PanelProperty = panelProp;
                if (p.PanelProperty != null ) p.PanelProperty.Material = materialProp;
                panelManager.Add(names[i], p);
            }

            panels = panelManager.GetRange(outIds);
            return outIds;
        }     
    }
}
