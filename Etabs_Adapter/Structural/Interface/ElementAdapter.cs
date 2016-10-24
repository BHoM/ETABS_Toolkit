using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM.Structural.Elements;
using BHoM.Structural.Interface;
using BHoM.Structural.Loads;
using ETABS2015;
using Etabs_Adapter.Structural.Elements;
using BHoM.Base;
using Etabs_Adapter.Structural.Loads;

namespace Etabs_Adapter.Structural.Interface
{
    public partial class EtabsAdapter : BHoM.Structural.Interface.IElementAdapter
    {
        private cOAPI Etabs;

        public string Filename
        {
            get; 
        }

        public EtabsAdapter(string filename = "")
        {
            string pathToETABS = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "Computers and Structures", "ETABS 2015", "ETABS.exe");
            System.Reflection.Assembly ETABSAssembly = System.Reflection.Assembly.LoadFrom(pathToETABS);

            object newInstance = ETABSAssembly.CreateInstance("CSI.ETABS.API.ETABSObject");

            if (newInstance != null)
            {
                Etabs = null;
                int attempt = 0;
                while (attempt++ <= 3)
                {
                    try
                    {
                        Etabs = (cOAPI)newInstance;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (attempt == 3)
                        {
                            throw ex;
                        }
                    }
                }
                if (Etabs != null)
                {
                    Etabs.ApplicationStart();

                    //if (hide)
                    //{
                    //    Etabs.Hide();
                    //}

                    cSapModel SapModel = Etabs.SapModel;
                    SapModel.InitializeNewModel(eUnits.N_m_C);

                    if (System.IO.File.Exists(filename))
                    {
                        SapModel.File.OpenFile(filename);
                        Filename = filename;
                    }
                    else
                    {
                        SapModel.File.NewBlank();
                        try
                        {
                            if (!string.IsNullOrEmpty(filename)) SapModel.File.Save(filename);
                            Filename = filename;
                        }
                        catch
                        {
                            Filename = "Unknown";
                        }
                    }
                }
            }
        }

        public ObjectSelection Selection { get; set; }

        public List<string> GetBars(out List<Bar> bars, List<string> ids = null)
        {
            return BarIO.GetBars(Etabs, out bars, Selection, ids);
        }

        public List<string> GetGrids(out List<Grid> grids, List<string> ids = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetLevels(out List<Storey> levels, List<string> ids = null)
        {
            return LevelIO.GetLevels(Etabs, out levels, ids);
        }

        public List<string> GetLoadcases(out List<ICase> cases)
        {
            return LoadIO.GetLoadcases(Etabs, out cases);
        }

        public bool GetLoads(out List<ILoad> loads, List<string> ids = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetNodes(out List<Node> nodes, List<string> ids = null)
        {
            return NodeIO.GetNodes(Etabs, out nodes, Selection, ids);
        }

        public List<string> GetOpenings(out List<Opening> opening, List<string> ids = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetPanels(out List<Panel> panels, List<string> ids = null)
        {
            return PanelIO.GetPanels(Etabs, out panels, Selection, ids);
        }

        public bool SetBars(List<Bar> bars, out List<string> ids)
        {
           return BarIO.SetBars(Etabs, bars, out ids);
        }

        public bool SetGrids(List<Grid> grid, out List<string> ids)
        {
            ids = new List<string>();
            return true;
        }

        public bool SetLevels(List<Storey> stories, out List<string> ids)
        {
            return LevelIO.SetLevels(Etabs, stories, out ids);
        }

        public bool SetLoadcases(List<ICase> cases)
        {
            throw new NotImplementedException();
        }

        public bool SetLoads(List<ILoad> loads)
        {
            throw new NotImplementedException();
        }

        public bool SetNodes(List<Node> nodes, out List<string> ids)
        {
            return NodeIO.CreateNodes(Etabs, nodes, out ids);
        }

        public bool SetOpenings(List<Opening> opening, out List<string> ids)
        {
            ids = new List<string>();
            return true;
        }

        public bool SetPanels(List<Panel> panels, out List<string> ids)
        {
            return PanelIO.SetPanels(Etabs, panels, out ids);
        }
        public List<string> GetRigidLinks(out List<RigidLink> links, List<string> ids = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetGroups(out List<IGroup> groups, List<string> ids = null)
        {
            throw new NotImplementedException();
        }

        public bool SetRigidLinks(List<RigidLink> rigidLinks, out List<string> ids)
        {
            throw new NotImplementedException();
        }

        public bool SetGroups(List<IGroup> groups, out List<string> ids)
        {
            throw new NotImplementedException();
        }

        public List<string> GetFEMeshes(out List<FEMesh> meshes, List<string> ids = null)
        {
            throw new NotImplementedException();
        }

        public bool SetFEMeshes(List<FEMesh> meshes, out List<string> ids)
        {
            throw new NotImplementedException();
        }
    }
}
