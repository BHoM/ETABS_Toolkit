/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SurfaceProperties;
using BH.Engine.Structure;
using BH.oM.Geometry;

namespace BH.Adapter.ETABS
{
#if Debug16 || Release16
    public partial class ETABS2016Adapter : BHoMAdapter
#elif Debug17 || Release17
   public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/

        private List<FEMesh> ReadMesh(List<string> ids = null)
        {
            List<Panel> panelList = new List<Panel>();
            int nameCount = 0;
            string[] nameArr = { };
            m_model.AreaObj.GetNameList(ref nameCount, ref nameArr);

            ids = FilterIds(ids, nameArr);

            List<FEMesh> meshes = new List<FEMesh>();
            Dictionary<string, Node> nodes = new Dictionary<string, Node>();
            Dictionary<string, ISurfaceProperty> surfaceProps = ReadSurfaceProperty().ToDictionary(x => GetAdapterId<string>(x));

            foreach (string id in ids)
            {
                FEMesh mesh = new FEMesh();

                ETABSId etabsid = new ETABSId();
                etabsid.Id = id;

                List<string> meshNodeIds = new List<string>();

                //Get out the "Element" ids, i.e. the mesh faces
                int nbELem = 0;
                string[] elemNames = new string[0];
                m_model.AreaObj.GetElm(id, ref nbELem, ref elemNames);

                for (int j = 0; j < nbELem; j++)
                {
                    //Get out the name of the points for each face
                    int nbPts = 0;
                    string[] ptsNames = new string[0];
                    m_model.AreaElm.GetPoints(elemNames[j], ref nbPts, ref ptsNames);

                    FEMeshFace face = new FEMeshFace();

                    for (int k = 0; k < nbPts; k++)
                    {
                        string nodeId = ptsNames[k];
                        Node node;

                        //Check if node already has been pulled
                        if (!nodes.TryGetValue(nodeId, out node))
                        {
                            double x = 0, y = 0, z = 0;
                            m_model.PointElm.GetCoordCartesian(nodeId, ref x, ref y, ref z);
                            node = new Node() { Position = new Point { X = x, Y = y, Z = z } };
                            SetAdapterId(node, nodeId);
                            nodes[ptsNames[k]] = node;
                        }

                        //Check if nodealready has been added to the mesh
                        if (!meshNodeIds.Contains(nodeId))
                            meshNodeIds.Add(nodeId);

                        //Get corresponding node index
                        face.NodeListIndices.Add(meshNodeIds.IndexOf(nodeId));
                    }

                    //Add face to list
                    SetAdapterId(face, elemNames[j]);
                    mesh.Faces.Add(face);

                }

                //Set mesh nodes - if there are no nodes, don't create the mesh.
                if (nodes.Count != 0 && mesh.Faces.Count != 0)
                {
                    mesh.Nodes = meshNodeIds.Select(x => nodes[x]).ToList();

                    string propertyName = "";

                    m_model.AreaObj.GetProperty(id, ref propertyName);

                    if (propertyName != "None")
                    {
                        mesh.Property = surfaceProps[propertyName];
                    }
                    
                    //Get local x-axis
                    double orientation = 0;
                    bool advanced = false;
                    m_model.AreaObj.GetLocalAxes(id, ref orientation, ref advanced);

                    Vector normal = mesh.Faces.First().Normal(mesh);    //Assuming flat mesh, all normals equal
                    Vector localX = Convert.FromCSILocalX(normal, orientation);
                    mesh = mesh.SetLocalOrientations(localX);

                    //Label and story
                    string label = "";
                    string story = "";
                    if (m_model.AreaObj.GetLabelFromName(id, ref label, ref story) == 0)
                    {
                        etabsid.Label = label;
                        etabsid.Story = story;
                    }

                    // Get guid
                    string guid = null;
                    m_model.AreaObj.GetGUID(id, ref guid);
                    etabsid.PersistentId = guid;

                    SetAdapterId(mesh, etabsid);
                    meshes.Add(mesh);
                }
                else
                {
                    BH.Engine.Base.Compute.RecordWarning("Mesh " + id.ToString() + " could not be pulled, because it contains no nodes");
                }
            }

            return meshes;
        }

        /***************************************************/
    }
}



