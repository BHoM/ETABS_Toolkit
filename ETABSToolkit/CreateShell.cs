using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;



namespace ETABSToolkit
{
    public class CreateShell
    {
        public static void SolveInstance(object etabs, List<Autodesk.DesignScript.Geometry.Mesh> Mesh, Boolean Activate = false)
        {
            Int32 ret = 0;
            if (Activate)
            {
                ETABS2015.cOAPI ETABS = (ETABS2015.cOAPI)etabs;
                
                for (int i = 0; i < Mesh.Count(); i++)
                {
                    Autodesk.DesignScript.Geometry.IndexGroup[] indexgroup = new Autodesk.DesignScript.Geometry.IndexGroup[Mesh[i].FaceIndices.Count()];
                    indexgroup = Mesh[i].FaceIndices;
                    foreach (Autodesk.DesignScript.Geometry.IndexGroup meshface in indexgroup)
                    {
                        List<Autodesk.DesignScript.Geometry.Point> pts = new List<Autodesk.DesignScript.Geometry.Point>();
                        pts.Add(Mesh[i].VertexPositions[meshface.A]);
                        pts.Add(Mesh[i].VertexPositions[meshface.B]);
                        pts.Add(Mesh[i].VertexPositions[meshface.C]);
                        if (meshface.Count > 3)
                        {
                            pts.Add(Mesh[i].VertexPositions[meshface.D]);
                        }

                        //commented out because index group does this for you could be useful for other tasks

                        //Autodesk.DesignScript.Geometry.Point[] pts = new Autodesk.DesignScript.Geometry.Point[Mesh[i].VertexPositions.Count()];
                        //pts = Mesh[i].VertexPositions;

                        ////order points based on centerpoint of mesh face and polar coordinates
                        //double average_x, average_y, average_z = 0;
                        //List<double> pt_x = new List<double>();
                        //List<double> pt_y = new List<double>();
                        //List<double> pt_z = new List<double>();
                        //List<double> theta = new List<double>();
                        //List<double> theta_unsorted = new List<double>();
                        //List<int> indices = new List<int>();
                        //Autodesk.DesignScript.Geometry.Point[] pts_srt = new Autodesk.DesignScript.Geometry.Point[Mesh[i].VertexPositions.Count()];
                        //for (int x = 0; x<pts.Count();x++)
                        //{
                        //    pt_x.Add(pts[x].X);
                        //    pt_y.Add(pts[x].Y);
                        //    pt_z.Add(pts[x].Z);
                        //}
                        //average_x = pt_x.Average();
                        //average_y = pt_y.Average();
                        //average_z = pt_z.Average();
                        //if (Mesh[i].VertexNormals[0].Z == 0) //exception for vertical walls
                        //{
                        //    for (int y = 0; y < pts.Count(); y++)
                        //    {
                        //        theta.Add(Math.Atan2((average_z - pt_z[y]), (average_x - pt_x[y])));
                        //    }

                        //}
                        //else
                        //{
                        //    for (int y = 0; y < pts.Count(); y++) //exception for non-vertical walls
                        //    {
                        //        theta.Add(Math.Atan2((average_y - pt_y[y]), (average_x - pt_x[y])));
                        //    }
                        //}
                        //foreach (double d in theta)
                        //{ theta_unsorted.Add(d); }
                        //theta.Sort();

                        //for (int z = 0; z < theta.Count;z++ )
                        //{ indices.Add(theta_unsorted.IndexOf(theta[z])); }

                        //for (int z = 0; z < theta.Count; z++)
                        //{ pts_srt[z]=(pts[indices[z]]);}


                        //I use arrays because the AddByCoord function can only handle arrays
                        double[] xValues = new double[pts.Count()];
                        double[] yValues = new double[pts.Count()];
                        double[] zValues = new double[pts.Count()];

                        for (int j = 0; j < pts.Count(); j++)
                        {
                            //Add all the x,y,z values from the mesh into arrays, used in AddByCoord function
                            xValues[j] = pts[j].X;
                            yValues[j] = pts[j].Y;
                            zValues[j] = pts[j].Z;
                        }
                        string Name = "Slabs" + i;
                        //Add the mesh in ETABS and refresh the view
                        ret = ETABS.SapModel.AreaObj.AddByCoord(pts.Count(), ref xValues, ref yValues, ref zValues, ref Name, "Slab1", "", "Global");
                        ret = ETABS.SapModel.View.RefreshView(0, true);

                       
                    }
                }
            }
        }
    }
}
