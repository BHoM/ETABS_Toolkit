using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM.Structural.Elements;
using BHoM.Structural.Interface;
using BHoM.Structural.Loads;
using ETABS2016;
using Etabs_Adapter.Structural.Elements;
using BHoM.Base;
using Etabs_Adapter.Structural.Loads;
using Etabs_Adapter.Base;
using System.IO;

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
            string pathToETABS = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "Computers and Structures", "ETABS 2016","ETABS.exe");
            //pathToETABS = "C:\\Users\\mhenriks\\AppData\\Roaming\\Grasshopper\\Libraries\\ETABS2016.dll";
            //System.Reflection.Assembly ETABSAssembly = System.Reflection.Assembly.LoadFrom(pathToETABS);

            //System.Reflection.Assembly[] assList = AppDomain.CurrentDomain.GetAssemblies();

            object newInstance = null;//ETABSAssembly.CreateInstance("CSI.ETABS.API.ETABSObject");
            newInstance = System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");
            int ret;

            Etabs = helper.GetObject(pathToETABS);

            if (Etabs == null)
            {
                object newInstance = helper.CreateObject(pathToETABS);

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
                }

                if (Etabs != null)
                {
                    //ret = Etabs.ApplicationStart();

                        //if (hide)
                        //{
                        //    Etabs.Hide();
                        //}

                    cSapModel SapModel = Etabs.SapModel;
                    SapModel.InitializeNewModel(eUnits.kN_m_C);

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

        public List<string> GetNodes(out List<Node> nodes, List<string> ids = null)
        {
            return NodeIO.GetNodes(Etabs, out nodes, Selection, ids);
        }

        public List<string> GetOpenings(out List<Opening> opening, List<string> ids = null)
        {
            return PanelIO.GetOpenings(Etabs, out opening, Selection);
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
            LoadIO.SetLoadcases(Etabs, cases);
            return true;
        }

        public bool SetLoads(List<ILoad> loads)
        {
            LoadIO.SetLoads(Etabs, loads);
            return true;
        }

        public bool SetNodes(List<Node> nodes, out List<string> ids)
        {
            return NodeIO.CreateNodes(Etabs, nodes, out ids);
        }

        public bool SetOpenings(List<Opening> opening, out List<string> ids)
        {
            return PanelIO.SetOpenings(Etabs, opening, out ids);
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
            ObjectManager<string, Bar> bars = new ObjectManager<string, Bar>(EtabsUtils.NUM_KEY, FilterOption.UserData);
            ObjectManager<string, Node> nodes = new ObjectManager<string, Node>(EtabsUtils.NUM_KEY, FilterOption.UserData);
            ObjectManager<string, IAreaElement> panels = new ObjectManager<string, IAreaElement>(EtabsUtils.NUM_KEY, FilterOption.UserData);
            List<string> groupsOut = new List<string>();

            int groupCount = 0;
            int numItems = 0;
            string[] groupNames = null;
            string[] objectNames = null;
            int[] objectType = null;
            groups = new List<IGroup>();
            Etabs.SapModel.GroupDef.GetNameList(ref groupCount, ref groupNames);
            List<IBase> groupObjs = new List<IBase>();
            int[] typeCount = new int[3];

            for (int i = 0; i < groupCount; i++)
            {
                Etabs.SapModel.GroupDef.GetAssignments(groupNames[i], ref numItems, ref objectType, ref objectNames);
                for (int j = 0; j < numItems; j++)
                {
                    switch (objectType[j])
                    {
                        case 1:
                            typeCount[0]++;
                            groupObjs.Add(nodes[objectNames[j]]);
                            break;
                        case 2:
                            typeCount[1]++;
                            groupObjs.Add(bars[objectNames[j]]);
                            break;
                        case 3:
                            typeCount[2]++;
                            groupObjs.Add(panels[objectNames[j]]);
                            break;
                    }
                }
                int type = 0;
                for (int typeIdx = 0; typeIdx < typeCount.Length; typeIdx++)
                {
                    if (typeCount[typeIdx] > 0)
                    {
                        if (type > 0)
                        {
                            type = 0; //More than one object type assigned to group
                            break;
                        }
                        else
                        {
                            type = typeIdx + 1;
                        }
                    }
                }

                switch (type)
                {
                    case 1:
                        groups.Add(new Group<Node>(groupNames[i], groupObjs.Cast<Node>().ToList()));
                        break;
                    case 2:
                        groups.Add(new Group<Bar>(groupNames[i], groupObjs.Cast<Bar>().ToList()));
                        break;
                    case 3:
                        groups.Add(new Group<IAreaElement>(groupNames[i], groupObjs.Cast<IAreaElement>().ToList()));
                        break;
                    default:
                        groups.Add(new Group<BHoMObject>(groupNames[i], groupObjs.Cast<BHoMObject>().ToList()));
                        break;
                }
                groupsOut.Add(groupNames[i]);
            }
            return groupsOut;
        }

        public bool SetRigidLinks(List<RigidLink> rigidLinks, out List<string> ids)
        {

            return RigidLinkIO.SetRidgidLinks(Etabs, rigidLinks, out ids);
        }

        public bool SetGroups(List<IGroup> groups, out List<string> ids)
        {
            ids = new List<string>();
            foreach (IGroup group in groups)
            {
                Etabs.SapModel.GroupDef.SetGroup(group.Name);
                ids.Add(group.Name);
                foreach (BHoMObject obj in group.Objects)
                {
                    object name = obj[Etabs_Adapter.Base.EtabsUtils.NUM_KEY];
                    if (name != null)
                    {
                        if (obj is Bar)
                        {
                            Etabs.SapModel.FrameObj.SetGroupAssign(name.ToString(), group.Name);
                        }
                        else if (typeof(IAreaElement).IsAssignableFrom(obj.GetType()))
                        {
                            Etabs.SapModel.AreaObj.SetGroupAssign(name.ToString(), group.Name);
                        }
                        else if (obj is Node)
                        {
                            Etabs.SapModel.PointObj.SetGroupAssign(name.ToString(), group.Name);
                        }
                        else if(obj is Panel)
                        {
                            Etabs.SapModel.AreaObj.SetGroupAssign(name.ToString(), group.Name);
                        }
                        else if(obj is RigidLink)
                        {
                            Etabs.SapModel.LinkObj.SetGroupAssign(name.ToString(), group.Name);
                        }
                    }

                }
            }
            return true;
        }

        public List<string> GetFEMeshes(out List<FEMesh> meshes, List<string> ids = null)
        {
            throw new NotImplementedException();
        }

        public bool SetFEMeshes(List<FEMesh> meshes, out List<string> ids)
        {
            return MeshIO.SetMesh(Etabs, meshes, out ids);
        }

        public bool GetLoads(out List<ILoad> loads, List<Loadcase> ids = null)
        {
            return LoadIO.GetLoads(Etabs, ids, out loads);
        }

        public bool Run()
        {
            throw new NotImplementedException();
        }
    }
}
