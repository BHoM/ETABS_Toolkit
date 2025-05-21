/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Constraints;
using BH.Engine.Adapters.ETABS;
#if Debug16 || Release16
using ETABS2016;
#elif Debug17 || Release17
using ETABSv17;
#else
using CSiAPIv1;
#endif

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

        private List<RigidLink> ReadRigidLink(List<string> ids = null)
        {
            List<RigidLink> linkList = new List<RigidLink>();

            int nameCount = 0;
            string[] names = { };
            m_model.LinkObj.GetNameList(ref nameCount, ref names);

            ids = FilterIds(ids, names);

            //read primary-multiSecondary nodes if these were initially created from (non-etabs)BHoM side
            Dictionary<string, List<string>> idDict = new Dictionary<string, List<string>>();
            string[] primarySecondaryId;

            foreach (string id in ids)
            {
                primarySecondaryId = id.Split(new[] { ":::" }, StringSplitOptions.None);
                if (primarySecondaryId.Count() > 1)
                {
                    //has plural secondaries
                    if (idDict.ContainsKey(primarySecondaryId[0]))
                        idDict[primarySecondaryId[0]].Add(primarySecondaryId[1]);
                    else
                        idDict.Add(primarySecondaryId[0], new List<string>() { primarySecondaryId[1] });
                }
                else
                {
                    //normal single link
                    idDict.Add(id, null);
                }
            }


            foreach (KeyValuePair<string, List<string>> kvp in idDict)
            {
                RigidLink bhLink = new RigidLink();
                SetAdapterId(bhLink, kvp.Key);

                if (kvp.Value == null)
                {
                    string startId = "";
                    string endId = "";
                    m_model.LinkObj.GetPoints(kvp.Key, ref startId, ref endId);

                    //Dummy nodes with correct Id
                    bhLink.PrimaryNode = new Node { Name = startId };
                    bhLink.SecondaryNodes = new List<Node>() { new Node { Name = endId } };
                }
                else
                {
                    string startId = "";
                    string endId = "";
                    string multiLinkId = kvp.Key + ":::0";


                    m_model.LinkObj.GetPoints(multiLinkId, ref startId, ref endId);
                    bhLink.PrimaryNode = new Node { Name = startId };   //Dummy startnode with correct Id

                    List<string> endIds = new List<string>();
                    for (int i = 1; i < kvp.Value.Count(); i++)
                    {
                        multiLinkId = kvp.Key + ":::" + i;
                        m_model.LinkObj.GetPoints(multiLinkId, ref startId, ref endId);
                        endIds.Add(endId);
                    }

                    bhLink.SecondaryNodes = endIds.Select(x => new Node { Name = x }).ToList(); //Dummy endnodes with correct Id
                }
                string propName = "";
                m_model.LinkObj.GetProperty(kvp.Key, ref propName);

                bhLink.Constraint = new LinkConstraint { Name = propName }; //Dummy constraint to be populated in later loop

                linkList.Add(bhLink);
            }

            if (linkList.Count == 0)
                return linkList;

            //Get ids of primary and secondary nodes
            List<string> nodeIds = linkList.SelectMany(x => x.SecondaryNodes.Select(s => s.Name).Concat(new List<string> { x.PrimaryNode.Name })).Distinct().ToList();
            Dictionary<string, Node> nodes = GetCachedOrReadAsDictionary<string, Node>(nodeIds);

            List<string> contstrainIds = linkList.Select(x => x.Constraint.Name).Distinct().ToList();
            //Get cached or read out all constraints used by Links
            Dictionary<string, LinkConstraint> constraints = contstrainIds.Any() ? new Dictionary<string, LinkConstraint>() : GetCachedOrReadAsDictionary<string, LinkConstraint>(contstrainIds);

            foreach (RigidLink link in linkList)
            {
                LinkConstraint constraint;  //Reasign cached/read contraint
                if (constraints.TryGetValue(link.Constraint.Name, out constraint))
                    link.Constraint = constraint;

                Node stNode;
                if (nodes.TryGetValue(link.PrimaryNode.Name, out stNode))
                    link.PrimaryNode = stNode;

                for (int i = 0; i < link.SecondaryNodes.Count; i++)
                {
                    Node secNode;
                    if (nodes.TryGetValue(link.SecondaryNodes[i].Name, out secNode))
                        link.SecondaryNodes[i] = secNode;
                }
            }


            return linkList;
        }

        /***************************************************/

        private List<LinkConstraint> ReadLinkConstraints(List<string> ids = null)
        {
            List<LinkConstraint> propList = new List<LinkConstraint>();
            int nameCount = 0;
            string[] names = { };
            m_model.PropLink.GetNameList(ref nameCount, ref names);

            ids = FilterIds(ids, names);

            foreach (string id in ids)
            {
                eLinkPropType linkType = eLinkPropType.Linear;
                m_model.PropLink.GetTypeOAPI(id, ref linkType);
                LinkConstraint constr = LinkConstraint(id, linkType);
                if (constr != null)
                    propList.Add(constr);
                else
                    Engine.Base.Compute.RecordError("Failed to read link constraint with id :" + id);

            }
            return propList;
        }

        /***************************************************/

        private LinkConstraint LinkConstraint(string name, eLinkPropType linkType)
        {

            switch (linkType)
            {
                case eLinkPropType.Linear:
                    return GetLinearLinkConstraint(name);
                case eLinkPropType.Damper:
                case eLinkPropType.Gap:
                case eLinkPropType.Hook:
                case eLinkPropType.PlasticWen:
                case eLinkPropType.Isolator1:
                case eLinkPropType.Isolator2:
                case eLinkPropType.MultilinearElastic:
                case eLinkPropType.MultilinearPlastic:
                case eLinkPropType.Isolator3:
                default:
                    Engine.Base.Compute.RecordError("Reading of LinkConstraint of type " + linkType + " not implemented");
                    return null;
            }

        }

        /***************************************************/

        private LinkConstraint GetLinearLinkConstraint(string name)
        {
            bool[] dof = null;
            bool[] fix = null;
            double[] stiff = null;
            double[] damp = null;
            double dj2 = 0; //Not sure what this is doing
            double dj3 = 0; //Not sure what this is doing
            bool stiffCoupled = false;
            bool dampCoupled = false;
            string notes = null;
            string guid = null;

            m_model.PropLink.GetLinear(name, ref dof, ref fix, ref stiff, ref damp, ref dj2, ref dj3, ref stiffCoupled, ref dampCoupled, ref notes, ref guid);

            LinkConstraint constraint = new LinkConstraint();

            constraint.Name = name;
            SetAdapterId(constraint, name);

            constraint.XtoX = fix[0];
            constraint.ZtoZ = fix[1];
            constraint.YtoY = fix[2];
            constraint.XXtoXX = fix[3];
            constraint.YYtoYY = fix[4];
            constraint.ZZtoZZ = fix[5];

            if (stiff != null && stiff.Any(x => x != 0))
                Engine.Base.Compute.RecordWarning("No stiffness read for link constraints");

            if (damp != null && damp.Any(x => x != 0))
                Engine.Base.Compute.RecordWarning("No damping read for link contraint");

            return constraint;

        }

        /***************************************************/
    }
}

