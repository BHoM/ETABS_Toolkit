using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Properties;
using BH.oM.Structure.Loads;
using BH.oM.Common.Materials;
using ETABS2016;
using BH.Engine.ETABS;
using BH.oM.Geometry;
using BH.Engine.Geometry;
using BH.Engine.Reflection;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        protected override IEnumerable<IBHoMObject> Read(Type type, IList ids)
        {
            if (type == typeof(Node))
                return ReadNodes(ids as dynamic);
            else if (type == typeof(Bar))
                return ReadBars(ids as dynamic);
            else if (type == typeof(ISectionProperty) || type.GetInterfaces().Contains(typeof(ISectionProperty)))
                return ReadSectionProperties(ids as dynamic);
            else if (type == typeof(Material))
                return ReadMaterials(ids as dynamic);
            else if (type == typeof(PanelPlanar))
                return ReadPanel(ids as dynamic);
            else if (type == typeof(IProperty2D))
                return ReadProperty2d(ids as dynamic);
            else if (type == typeof(LoadCombination))
                return ReadLoadCombination(ids as dynamic);
            else if (type == typeof(Loadcase))
                return ReadLoadcase(ids as dynamic);
            else if (type == typeof(ILoad) || type.GetInterfaces().Contains(typeof(ILoad)))
                return ReadLoad(type, ids as dynamic);
            else if (type == typeof(RigidLink))
                return ReadRigidLink(ids as dynamic);


            return new List<IBHoMObject>();//<--- returning null will throw error in replace method of BHOM_Adapter line 34: can't do typeof(null) - returning null does seem the most sensible to return though
        }

        private List<Node> ReadNodes(List<string> ids = null)
        {
            List<Node> nodeList = new List<Node>();

            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                m_model.PointObj.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            foreach (string id in ids)
            {
                Node bhNode = new Node();
                double x, y, z;
                x = y = z = 0;
                bool[] restraint = new bool[6];
                double[] spring = new double[6];

                m_model.PointObj.GetCoordCartesian(id, ref x, ref y, ref z);
                bhNode.Position = new oM.Geometry.Point() { X = x, Y = y, Z = z };
                bhNode.CustomData.Add(AdapterId, id);

                m_model.PointObj.GetRestraint(id, ref restraint);
                m_model.PointObj.SetSpring(id, ref spring);
                bhNode.Constraint = Helper.GetConstraint6DOF(restraint, spring);


                nodeList.Add(bhNode);
            }


            return nodeList;
        }

        private List<Bar> ReadBars(List<string> ids = null)
        {
            List<Bar> barList = new List<Bar>();
            int nameCount = 0;
            string[] names = { };

            if (ids == null)
            {
                m_model.FrameObj.GetNameList(ref nameCount, ref names);
                ids = names.ToList();
            }

            //Storing the sectionproperties as they are being pulled out, to only pull each section once.
            Dictionary<string, ISectionProperty> sectionProperties = new Dictionary<string, ISectionProperty>();

            foreach (string id in ids)
            {
                try
                {
                    Bar bhBar = new Bar();
                    bhBar.CustomData.Add(AdapterId, id);
                    string startId = "";
                    string endId = "";
                    m_model.FrameObj.GetPoints(id, ref startId, ref endId);

                    List<Node> endNodes = ReadNodes(new List<string> { startId, endId });
                    bhBar.StartNode = endNodes[0];
                    bhBar.EndNode = endNodes[1];

                    bool[] restraintStart = new bool[6];
                    double[] springStart = new double[6];
                    bool[] restraintEnd = new bool[6];
                    double[] springEnd = new double[6];

                    m_model.FrameObj.GetReleases(id, ref restraintStart, ref restraintEnd, ref springStart, ref springEnd);
                    bhBar.Release = new BarRelease();
                    bhBar.Release.StartRelease = Helper.GetConstraint6DOF(restraintStart, springStart);
                    bhBar.Release.EndRelease = Helper.GetConstraint6DOF(restraintEnd, springEnd);

                    eFramePropType propertyType = eFramePropType.General;
                    string propertyName = "";
                    string sAuto = "";
                    m_model.FrameObj.GetSection(id, ref propertyName, ref sAuto);
                    if (propertyName != "None")
                    {
                        ISectionProperty property;

                        //Check if section allready has been pulled once
                        if (!sectionProperties.TryGetValue(propertyName, out property))
                        {
                            //if not pull it and store it
                            m_model.PropFrame.GetTypeOAPI(propertyName, ref propertyType);
                            property = Helper.GetSectionProperty(m_model, propertyName, propertyType);
                            sectionProperties[propertyName] = property;
                        }

                        bhBar.SectionProperty = property;
                    }

                    //bool autoOffset = false;
                    //double startLength = 0;
                    //double endLength = 0;
                    //double rz = 0;
                    //model.FrameObj.GetEndLengthOffset(id, ref autoOffset, ref startLength, ref endLength, ref rz);
                    //if (!autoOffset)
                    //{
                    //    bhBar.Offset.Start = startLength == 0 ? null : new Vector() { X = startLength * (-1), Y = 0, Z = 0 };
                    //    bhBar.Offset.End = endLength == 0 ? null : new Vector() { X = endLength, Y = 0, Z = 0 };
                    //}
                    barList.Add(bhBar);
                }
                catch
                {
                    BH.Engine.Reflection.Compute.RecordError("Bar " + id.ToString() + " could not be pulled");
                }
            }
            return barList;
        }

        private List<ISectionProperty> ReadSectionProperties(List<string> ids = null)
        {
            List<ISectionProperty> propList = new List<ISectionProperty>();
            int nameCount = 0;
            string[] names = { };

            if (ids == null)
            {
                m_model.PropFrame.GetNameList(ref nameCount, ref names);
                ids = names.ToList();
            }

            eFramePropType propertyType = eFramePropType.General;

            foreach (string id in ids)
            {
                m_model.PropFrame.GetTypeOAPI(id, ref propertyType);
                propList.Add(Helper.GetSectionProperty(m_model, id, propertyType));
            }
            return propList;
        }

        private List<Material> ReadMaterials(List<string> ids = null)
        {
            int nameCount = 0;
            string[] names = { };
            List<Material> materialList = new List<Material>();

            if (ids == null)
            {
                m_model.PropMaterial.GetNameList(ref nameCount, ref names);
                ids = names.ToList();
            }

            foreach (string id in ids)
            {
                materialList.Add(Helper.GetMaterial(m_model, id));
            }

            return materialList;
        }

        private List<IProperty2D> ReadProperty2d(List<string> ids = null)
        {
            List<IProperty2D> propertyList = new List<IProperty2D>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                m_model.PropArea.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            foreach (string id in ids)
            {
                IProperty2D bhProperty = null;
                eSlabType slabType = eSlabType.Slab;
                eShellType shellType = eShellType.ShellThin;
                string material = "";
                double thickness = 0;
                int colour = 0;
                string notes = "";
                string guid = "";
                double depth = 0;
                double stemWidthTop = 0;
                double stemWidthBottom = 0;//not used
                double ribSpacing = 0;
                double ribSpacing2nd = 0;
                int direction = 0;
                double[] modifiers = new double[] { };
                bool hasModifiers = false;

                m_model.PropArea.GetSlab(id, ref slabType, ref shellType, ref material, ref thickness, ref colour, ref notes, ref guid);
                if (m_model.PropArea.GetModifiers(id, ref modifiers) == 0)
                    hasModifiers = true;

                if (thickness==0)
                {
                    eWallPropType wallProperty = eWallPropType.AutoSelectList;
                    m_model.PropArea.GetWall(id, ref wallProperty, ref shellType, ref material, ref thickness, ref colour, ref notes, ref guid);
                    ConstantThickness panelConstant = new ConstantThickness();
                    panelConstant.CustomData[AdapterId] = id;
                    panelConstant.Material = ReadMaterials(new List<string>() { material })[0];
                    panelConstant.Thickness = thickness;
                    panelConstant.PanelType = PanelType.Wall;
                    if(hasModifiers)
                        panelConstant.CustomData.Add("Modifiers", modifiers);

                    propertyList.Add(panelConstant);
                }
                else
                {
                    switch (slabType)
                    {
                        case eSlabType.Ribbed:
                            Ribbed panelRibbed = new Ribbed();

                            m_model.PropArea.GetSlabRibbed(id, ref depth, ref thickness, ref stemWidthTop, ref stemWidthBottom, ref ribSpacing, ref direction);
                            panelRibbed.Name = id;
                            panelRibbed.CustomData[AdapterId] = id;
                            panelRibbed.Material = ReadMaterials(new List<string>() { material })[0];
                            panelRibbed.Thickness = thickness;
                            panelRibbed.PanelType = PanelType.Slab;
                            panelRibbed.Direction = (PanelDirection)direction;
                            panelRibbed.Spacing = ribSpacing;
                            panelRibbed.StemWidth = stemWidthTop;
                            panelRibbed.TotalDepth = depth;
                            if (hasModifiers)
                                panelRibbed.CustomData.Add("Modifiers", modifiers);

                            propertyList.Add(panelRibbed);
                            break;
                        case eSlabType.Waffle:
                            Waffle panelWaffle = new Waffle();

                            m_model.PropArea.GetSlabWaffle(id, ref depth, ref thickness, ref stemWidthTop, ref stemWidthBottom, ref ribSpacing, ref ribSpacing2nd);
                            panelWaffle.Name = id;
                            panelWaffle.CustomData[AdapterId] = id;
                            panelWaffle.Material = ReadMaterials(new List<string>() { material })[0];
                            panelWaffle.SpacingX = ribSpacing;
                            panelWaffle.SpacingY = ribSpacing2nd;
                            panelWaffle.StemWidthX = stemWidthTop;
                            panelWaffle.StemWidthY = stemWidthTop; //ETABS does not appear to support direction dependent stem width
                            panelWaffle.Thickness = thickness;
                            panelWaffle.TotalDepthX = depth;
                            panelWaffle.TotalDepthY = depth; // ETABS does not appear to to support direction dependent depth
                            panelWaffle.PanelType = PanelType.Slab;
                            if (hasModifiers)
                                panelWaffle.CustomData.Add("Modifiers", modifiers);

                            propertyList.Add(panelWaffle);
                            break;
                        case eSlabType.Slab:
                        case eSlabType.Drop:
                        case eSlabType.Stiff_DO_NOT_USE:
                        default:
                            ConstantThickness panelConstant = new ConstantThickness();
                            panelConstant.CustomData[AdapterId] = id;
                            panelConstant.Name = id;
                            panelConstant.Material = ReadMaterials(new List<string>() { material })[0];
                            panelConstant.Thickness = thickness;
                            panelConstant.Name = id;
                            panelConstant.PanelType = PanelType.Slab;
                            if (hasModifiers)
                                panelConstant.CustomData.Add("Modifiers", modifiers);

                            propertyList.Add(panelConstant);
                            break;
                    }
                }
            }

            return propertyList;
        }

        private List<PanelPlanar> ReadPanel(List<string> ids = null)
        {
            List<PanelPlanar> panelList = new List<PanelPlanar>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                m_model.AreaObj.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            //get openings, if any
            m_model.AreaObj.GetNameList(ref nameCount, ref nameArr);
            bool isOpening = false;
            Dictionary<string, Polyline> openingDict = new Dictionary<string, Polyline>();
            foreach (string name in nameArr)
            {
                m_model.AreaObj.GetOpening(name, ref isOpening);
                if (isOpening)
                {
                    openingDict.Add(name, Helper.GetPanelPerimeter(m_model,name));
                }
            }

            foreach (string id in ids)
            {
                if (openingDict.ContainsKey(id))
                    continue;

                string propertyName = "";

                m_model.AreaObj.GetProperty(id, ref propertyName);
                IProperty2D panelProperty = ReadProperty2d(new List<string>() { propertyName })[0];

                PanelPlanar panel = new PanelPlanar();
                panel.CustomData[AdapterId] = id;
                
                Polyline pl = Helper.GetPanelPerimeter(m_model, id);

                Edge edge = new Edge();
                edge.Curve = pl;
                //edge.Constraint = new Constraint4DOF();// <---- cannot see anyway to set this via API and for some reason constraints are not being set in old version of etabs toolkit TODO

                panel.ExternalEdges = new List<Edge>() { edge };
                foreach(KeyValuePair<string, Polyline> kvp in openingDict)
                {
                    if (pl.IsContaining(kvp.Value.ControlPoints))
                    {
                        Opening opening = new Opening();
                        opening.Edges = new List<Edge>() { new Edge() { Curve = kvp.Value } };
                        panel.Openings.Add(opening);
                    }
                }

                panel.Property = panelProperty;

                panelList.Add(panel);
            }

            return panelList;
        }

        private List<LoadCombination> ReadLoadCombination(List<string> ids = null)
        {
            List<LoadCombination> combinations = new List<LoadCombination>();

            //get all load cases before combinations
            int number = 0;
            string[] names = null;
            m_model.LoadPatterns.GetNameList(ref number, ref names);
            Dictionary<string, ICase> caseDict = new Dictionary<string, ICase>();

            //ensure id can be split into name and number
            names = Helper.EnsureNameWithNum(names.ToList()).ToArray();

            foreach (string name in names)
                caseDict.Add(name, Helper.GetLoadcase(m_model, name));

            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                m_model.RespCombo.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            //ensure id can be split into name and number
            ids = Helper.EnsureNameWithNum(ids);

            foreach (string id in ids)
            {
                combinations.Add(Helper.GetLoadCombination(m_model, caseDict, id));
            }

            return combinations;
        }

        private List<Loadcase> ReadLoadcase(List<string> ids = null)
        {
            int nameCount = 0;
            string[] nameArr = { };

            List<Loadcase> loadcaseList = new List<Loadcase>();

            if (ids == null)
            {
                m_model.LoadPatterns.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            //ensure id can be split into name and number
            ids = Helper.EnsureNameWithNum(ids);

            foreach (string id in ids)
            {
                loadcaseList.Add(Helper.GetLoadcase(m_model, id));
            }

            return loadcaseList;
        }

        private List<ILoad> ReadLoad(Type type, List<string> ids = null)
        {
            List<ILoad> loadList = new List<ILoad>();

            //get loadcases first
            List<Loadcase> loadcaseList = ReadLoadcase();

            loadList = Helper.GetLoads(m_model, loadcaseList);

            //filter the list to return only the right type - No, this is not a clever way of doing it !
            loadList = loadList.Where(x => x.GetType() == type).ToList();

            return loadList;
        }

        private List<RigidLink> ReadRigidLink(List<string> ids = null)
        {
            List<RigidLink> linkList = new List<RigidLink>();

            int nameCount = 0;
            string[] names = { };

            if (ids == null)
            {
                m_model.LinkObj.GetNameList(ref nameCount, ref names);
                ids = names.ToList();
            }

            //read master-multiSlave nodes if these were initially created from (non-etabs)BHoM side
            Dictionary<string, List<string>> idDict = new Dictionary<string, List<string>>();
            string[] masterSlaveId;

            foreach (string id in ids)
            {
                masterSlaveId = id.Split(new[] { ":::" }, StringSplitOptions.None);
                if (masterSlaveId.Count()>1)
                {
                    //has multi slaves
                    if (idDict.ContainsKey(masterSlaveId[0]))
                        idDict[masterSlaveId[0]].Add(masterSlaveId[1]);
                    else
                        idDict.Add(masterSlaveId[0], new List<string>() { masterSlaveId[1] });
                }
                else
                {
                    //normal single link
                    idDict.Add(id, null);
                }
            }


            foreach (KeyValuePair<string,List<string>> kvp in idDict)
            {
                RigidLink bhLink = new RigidLink();

                if (kvp.Value == null)
                {
                    bhLink.CustomData.Add(AdapterId, kvp.Key);
                    string startId = "";
                    string endId = "";
                    m_model.LinkObj.GetPoints(kvp.Key, ref startId, ref endId);

                    List<Node> endNodes = ReadNodes(new List<string> { startId, endId });
                    bhLink.MasterNode= endNodes[0];
                    bhLink.SlaveNodes = new List<Node>() { endNodes[1] };

                    linkList.Add(bhLink);
                }
                else
                {
                    
                    bhLink.CustomData.Add(AdapterId, kvp.Key);
                    string startId = "";
                    string endId = "";
                    string multiLinkId = kvp.Key + ":::0";
                    List<string> nodeIdsToRead = new List<string>();

                    m_model.LinkObj.GetPoints(multiLinkId, ref startId, ref endId);
                    nodeIdsToRead.Add(startId);

                    for (int i = 1; i<kvp.Value.Count();i++)
                    {
                        multiLinkId = kvp.Key + ":::" + i;
                        m_model.LinkObj.GetPoints(multiLinkId, ref startId, ref endId);
                        nodeIdsToRead.Add(endId);
                    }

                    List<Node> endNodes = ReadNodes(nodeIdsToRead);
                    bhLink.MasterNode = endNodes[0];
                    endNodes.RemoveAt(0);
                    bhLink.SlaveNodes = endNodes;

                    linkList.Add(bhLink);
                }
            }

            return linkList;
        }
    }
}
