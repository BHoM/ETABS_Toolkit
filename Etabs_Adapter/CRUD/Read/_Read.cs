/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2024, the respective contributors. All rights reserved.
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
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Analytical.Results;
using BH.oM.Adapter;
using System.ComponentModel;
using BH.oM.Data.Requests;

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
        /*** Private methods - Read                      ***/
        /***************************************************/

        protected override IEnumerable<IBHoMObject> IRead(Type type, IList ids, ActionConfig actionConfig = null)
        {
            List<string> listIds = null;
            if (ids != null && ids.Count != 0)
                listIds = ids.Cast<string>().ToList();

            if (type == typeof(Node))
                return ReadNode(listIds);
            else if (type == typeof(Bar))
                return ReadBar(listIds);
            else if (type == typeof(ISectionProperty) || type.GetInterfaces().Contains(typeof(ISectionProperty)))
                return ReadSectionProperty(listIds);
            else if (type == typeof(IMaterialFragment) || type.GetInterfaces().Contains(typeof(IMaterialFragment)))
                return ReadMaterial(listIds);
            else if (type == typeof(Panel))
                return ReadPanel(listIds);
            else if (type == typeof(ISurfaceProperty) || type.GetInterfaces().Contains(typeof(ISurfaceProperty)))
                return ReadSurfaceProperty(listIds);
            else if (type == typeof(LoadCombination))
                return ReadLoadCombination(listIds);
            else if (type == typeof(Loadcase))
                return ReadLoadcase(listIds);
            else if (type == typeof(ILoad) || type.GetInterfaces().Contains(typeof(ILoad)))
                return ReadLoad(type, listIds);
            else if (type == typeof(RigidLink))
                return ReadRigidLink(listIds);
            else if (type == typeof(LinkConstraint))
                return ReadLinkConstraints(listIds);
            else if (type == typeof(oM.Spatial.SettingOut.Level))
                return ReadLevel(listIds);
            else if (type == typeof(oM.Spatial.SettingOut.Grid))
                return ReadGrid(listIds);
            else if (type == typeof(FEMesh))
                return ReadMesh(listIds);
            else if (typeof(IResult).IsAssignableFrom(type))
            {
                ReadResultsError(type);
                return null;
            }

            return new List<IBHoMObject>();//<--- returning null will throw error in replace method of BHOM_Adapter line 34: can't do typeof(null) - returning null does seem the most sensible to return though
        }

        /***************************************************/

        public IEnumerable<IBHoMObject> Read <T>(T request, ActionConfig actionConfig = null) where T : ILogicalRequest
        {
            // The implementation must:
            // 1. extract all the needed info from the IRequest
            // 2. return a call to the Basic Method Read() with the extracted info.

            HashSet < IBHoMObject > bhomObjects= new HashSet<IBHoMObject >();

            if (request is LogicalAndRequest)
            {
                List<IRequest> requests = (request as LogicalAndRequest).Requests;
                
                IRequest req = requests[0];
                if (req.GetType() is FilterRequest) Read((FilterRequest)req, actionConfig).ToList().ForEach(bhomObj => bhomObjects.Add(bhomObj));
                if (req.GetType() is SelectionRequest) Read((SelectionRequest)req, actionConfig).ToList().ForEach(bhomObj => bhomObjects.Add(bhomObj));
                if (req.GetType() is ILogicalRequest) Read<ILogicalRequest>((ILogicalRequest)req, actionConfig);

                for (int i = 1; i<requests.Count; i++)
                {
                    if (requests[i].GetType() is FilterRequest) bhomObjects=bhomObjects.Intersect(Read((FilterRequest)req, actionConfig)).ToHashSet();
                    if (requests[i].GetType() is SelectionRequest) bhomObjects = bhomObjects.Intersect(Read((FilterRequest)req, actionConfig)).ToHashSet();
                    if (requests[i].GetType() is ILogicalRequest) Read<ILogicalRequest>((ILogicalRequest)req, actionConfig);
                }

                return bhomObjects;

            }

            else if (request is LogicalOrRequest)

            {
                List<IRequest> requests = (request as LogicalOrRequest).Requests;
                requests.ForEach(req => { if (req.GetType()==typeof(FilterRequest)) Read((FilterRequest)req, actionConfig).ToList().ForEach(bhomObj => bhomObjects.Add(bhomObj));
                                          if (req.GetType() == typeof(SelectionRequest)) Read((SelectionRequest)req, actionConfig).ToList().ForEach(bhomObj => bhomObjects.Add(bhomObj));
                                          if (req.GetType().IsSubclassOf(typeof(ILogicalRequest))) Read<ILogicalRequest>((ILogicalRequest)req, actionConfig);});
                return bhomObjects;
            }

            //else if (request is LogicalNotRequest)
            //{

            //}

            else
            {
                BH.Engine.Base.Compute.RecordError($"Requests of type {request?.GetType()} are not supported by the Excel adapter.");
                return new List<IBHoMObject>();
            }

            BH.Engine.Base.Compute.RecordError($"Read for {request.GetType().Name} is not implemented in {(this as dynamic).GetType().Name}.");
            return new List<IBHoMObject>();
        }


        public IEnumerable<IBHoMObject> Read(FilterRequest filterRequest, ActionConfig actionConfig = null)
        {
            // Extract the Ids from the FilterRequest
            IList objectIds = null;
            object idObject;
            if (filterRequest.Equalities.TryGetValue("ObjectIds", out idObject) && idObject is IList)
                objectIds = idObject as IList;

            return IRead(filterRequest.Type, objectIds, actionConfig);
        }



        /***************************************************/

        public IEnumerable<IBHoMObject> Read(SelectionRequest request, ActionConfig actionConfig = null)
        {
            List<IBHoMObject> results = new List<IBHoMObject>();

            foreach (KeyValuePair<Type, List<string>> keyVal in SelectedElements())
            {
                results.AddRange(IRead(keyVal.Key, keyVal.Value, actionConfig));
            }

            return results;
        }

        /***************************************************/

        public Dictionary<Type, List<string>> SelectedElements()
        {
            int numItems = 0;
            int[] objectTypes = new int[0];
            string[] objectIds = new string[0];

            m_model.SelectObj.GetSelected(ref numItems, ref objectTypes, ref objectIds);

            Dictionary<int, List<string>> dict = objectTypes.Distinct().ToDictionary(x => x, x => new List<string>());

            for (int i = 0; i < numItems; i++)
            {
                dict[objectTypes[i]].Add(objectIds[i]);
            }

            Func<int, Type> ToType = x =>
            {
                switch (x)
                {
                    case 1: // Point Object
                        return typeof(Node);
                    case 2: // Frame Object
                    case 3: // Cable Object
                    case 4: // Tendon Object
                        return typeof(Bar);
                    case 5: // Area Object
                        return typeof(Panel);
                    case 6: // Solid Object
                        return null;
                    case 7: // Link Object
                        return typeof(RigidLink);
                    default:
                        return null;
                }
            };

            return dict.ToDictionary(x => ToType(x.Key), x => x.Value);
        }

        /***************************************************/

        [Description("Ensures that all elements in the first list are present in the second list, warning if not, and returns the second list if the first list is empty.")]
        private static List<string> FilterIds(IEnumerable<string> ids, IEnumerable<string> etabsIds)
        {
            if (ids == null || ids.Count() == 0)
            {
                return etabsIds.ToList();
            }
            else
            {
                List<string> result = ids.Intersect(etabsIds).ToList();
                if (result.Count() != ids.Count())
                    Engine.Base.Compute.RecordWarning("Some requested ETABS ids were not present in the model.");
                return result;
            }
        }

        /***************************************************/

    }
}





