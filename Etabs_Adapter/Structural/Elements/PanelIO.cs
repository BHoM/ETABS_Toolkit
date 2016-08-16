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
                Group<Curve> c = panel.External_Contours;
                string currentThickness = "";
                try
                {
                    if (c != null)
                    {
                        if (panel.PanelProperty != null && !addedThicknesses.TryGetValue(panel.PanelProperty.Name, out currentThickness))
                        {
                            PropertyIO.CreatePanelProperty(SapModel, EtabsUtils.IsVertical(panel.External_Contours[0]), panel.PanelProperty, panel.Material);
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
                            if (j > edgeCount)
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

    }
}
