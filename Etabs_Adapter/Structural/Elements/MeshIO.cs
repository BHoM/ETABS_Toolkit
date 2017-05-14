using BHoM.Geometry;
using BHoM.Materials;
using BHoM.Structural.Elements;
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
    public class MeshIO
    {

        public static bool SetMesh(cOAPI Etabs, List<FEMesh> meshes, out List<string> ids)
        {
            cSapModel SapModel = Etabs.SapModel;
            ids = new List<string>();
            Dictionary<string, PanelProperty> addedProperties = new Dictionary<string, PanelProperty>();
            Dictionary<string, Material> addedMaterials = new Dictionary<string, Material>();
            PanelProperty panelProp = null;
            Material materialProp = null;
            string propertyName = "";
            string material = "";
            string name = "";

            for (int i = 0; i < meshes.Count; i++)
            {
                if (!addedProperties.TryGetValue(propertyName, out panelProp))
                {
                    panelProp = PropertyIO.GetPanelProperty(SapModel, propertyName, out material);
                    addedProperties.Add(propertyName, panelProp);
                    addedMaterials.Add(propertyName, EtabsUtils.GetMaterial(SapModel, material));
                }          
                
                List<Node> vertices = meshes[i].Nodes;                 

                for (int j = 0; j < meshes[i].Faces.Count; j++)
                {
                    FEFace face = meshes[i].Faces[j];
                    int vertexCount = face.IsQuad ? 4 : 3;

                    double[] x = new double[vertexCount];
                    double[] y = new double[vertexCount];
                    double[] z = new double[vertexCount];
                    
                    for (int k = 0; k < face.NodeIndices.Count; k++)
                    {
                        Node n = vertices[face.NodeIndices[k]];
                        x[k] = n.X;
                        y[k] = n.Y;
                        z[k] = n.Z;
                    }
                    
                    SapModel.AreaObj.AddByCoord(vertexCount, ref x, ref y, ref z, ref name, propertyName);

                    ids.Add(name);
                }               
            }
            return true;
        }
    }
}
