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
using System.Reflection;
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
using BH.oM.Spatial.SettingOut;
using BH.Adapter.ETABS.Types;

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
            else if (type == typeof(Opening))
                return ReadOpening(listIds);
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

        public IEnumerable<IBHoMObject> Read <T>(T request, ActionConfig actionConfig = null) where T : ILogicalRequest //- **GENERIC TYPES**
        {

            // Use of GENERIC TYPES, HASH TABLES, STREAMS and RECURSION

            /* Use a HashSet data structure to make sure no collected elements are duplicate and to make access/search as fast as possible - **HASH TABLES ** */
            HashSet < IBHoMObject > bhomObjects= new HashSet<IBHoMObject >();

            /* 1. Handle the LogicalANDRequest */
            if (request is LogicalAndRequest)
            {
                // 1.1 Initialize List of Requests to be extracted from LogicalRequest
                List<IRequest> requests = (request as LogicalAndRequest).Requests;
                // 1.2 Initialize DynamicComparer class instance allowing to check equality between IBHoMObject class instances
                DynamicComparer iBHoMETABSComparer = new DynamicComparer();

                // 1.3 Add to bhomObjects List all objects abiding by the FIRST request in the list... - **STREAMS**
                IRequest req = requests[0];
                // ...when it's a FilterRequest...
                if (req.GetType() == typeof(FilterRequest)) Read((FilterRequest)req, actionConfig).ToList().ForEach(bhomObj => bhomObjects.Add(bhomObj));
                // ...when it's a SelectionRequest...
                if (req.GetType() == typeof(SelectionRequest)) Read((SelectionRequest)req, actionConfig).ToList().ForEach(bhomObj => bhomObjects.Add(bhomObj));
                // ...when it's a LogicalRequest...call the method recursively! - **RECURSION**
                if (req is ILogicalRequest) bhomObjects=Read<ILogicalRequest>((ILogicalRequest)req, actionConfig).ToHashSet();


                // 1.4 Add to bhomObjects List all objects abiding by ALL THE OTHER requests in the list... - **STREAMS**
                for (int i = 1; i < requests.Count; i++)
                {
                    // ...when they are FilterRequests...
                    if (requests[i].GetType() == typeof(FilterRequest)) bhomObjects = (bhomObjects.ToList().Intersect(Read((FilterRequest)requests[i], actionConfig).ToList(), iBHoMETABSComparer)).ToHashSet();
                    // ...when they are SelectionRequests...
                    if (requests[i].GetType() == typeof(SelectionRequest)) bhomObjects = (bhomObjects.ToList().Intersect(Read((SelectionRequest)requests[i], actionConfig).ToList(), iBHoMETABSComparer)).ToHashSet();
                    // ...when they are LogicalRequests...call the method recursively! - **RECURSION**
                    if (requests[i] is ILogicalRequest) bhomObjects = (bhomObjects.ToList().Intersect(Read<ILogicalRequest>((ILogicalRequest)requests[i], actionConfig))).ToHashSet();
                }

                // 1.5 Return list of bhomObjects
                return bhomObjects;
            }

            /* 2. Handle the LogicalORRequest */
            else if (request is LogicalOrRequest)
            {
                // 2.1 Initialize List of Requests to be extracted from LogicalRequest
                List<IRequest> requests = (request as LogicalOrRequest).Requests;

                // 2.2 Add to bhomObjects List all objects abiding by ALL requests in the list... - **STREAMS**
                // ...when they are FilterRequests...
                requests.ForEach(req => { if (req.GetType() == typeof(FilterRequest)) Read((FilterRequest)req, actionConfig).ToList().ForEach(bhomObj => bhomObjects.Add(bhomObj));
                    // ...when they are SelectionRequests...                    
                    if (req.GetType() == typeof(SelectionRequest)) Read((SelectionRequest)req, actionConfig).ToList().ForEach(bhomObj => bhomObjects.Add(bhomObj));
                    // ...when they are LogicalRequests...call the method recursively! - **RECURSION**                    
                    if (req is ILogicalRequest) bhomObjects=Read<ILogicalRequest>((ILogicalRequest)req, actionConfig).ToHashSet(); });

                // 2.3 Return list of bhomObjects                
                return bhomObjects;
            }

            /* 3. Handle the LogicalNOTRequest */
            else if (request is LogicalNotRequest)
            {
                // 3.1 Initialize Lists and Hashsets for collecting all bhomObjects - **HASH TABLES **
                IRequest iRequest = (request as LogicalNotRequest).Request;
                HashSet<IBHoMObject> notBhomObjects = new HashSet<IBHoMObject>();
                List<IBHoMObject> allBhomObjects = new List<IBHoMObject>();

                // 3.2 Add to NOTbhomObjects HashSet all unique objects abiding by the Request input in the LogicalNOTRequest... - **STREAMS**
                // ...when it's a FilterRequest...
                if (iRequest.GetType() == typeof(FilterRequest)) Read((FilterRequest)iRequest, actionConfig).ToList().ForEach(bhomObj => notBhomObjects.Add(bhomObj));
                // ...when it's a SelectionRequest...                
                if (iRequest.GetType() == typeof(SelectionRequest)) Read((SelectionRequest)iRequest, actionConfig).ToList().ForEach(bhomObj => notBhomObjects.Add(bhomObj));
                // ...when it's a LogicalRequest...call the method recursively! - **RECURSION**               
                if (iRequest is ILogicalRequest) Read<ILogicalRequest>((ILogicalRequest)iRequest, actionConfig).ToList().ForEach(bhomObj => notBhomObjects.Add(bhomObj));


                // 3.3 Get all bhomObjects of ANY kind from ETABS Model... - **STREAMS**
                Type[] bhomTypes = {typeof(Node),typeof(Bar),typeof(ISectionProperty), typeof(IMaterialFragment), typeof(Panel),
                                   typeof(ISurfaceProperty), typeof(LoadCombination), typeof(Loadcase), typeof(ILoad), typeof(RigidLink),
                                   typeof(LinkConstraint),typeof(oM.Spatial.SettingOut.Level),typeof(oM.Spatial.SettingOut.Grid),typeof(FEMesh)};

                allBhomObjects = bhomTypes.ToList()
                                          .Select(bhomType =>{ FilterRequest fr = new FilterRequest();
                                                              fr.Type = bhomType;
                                                              return fr;})
                                          .Select(filtReq => Read(filtReq))
                                          .SelectMany(x=>x) // Streams function allowing to flatten multidimensional lists!
                                          .ToList();

                // 3.4 Return the difference between ALL Objects and the ones NOT to be taken - **STREAMS**
                // - .Except() Streams function returns the difference between two lists/data structures based on a specified EqualityComparer class)
                return allBhomObjects.Except(notBhomObjects,new DynamicComparer());
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

            // Replace Panels' type numbers with Openings' type numbers
            
            for (int i=0; i<numItems; i++)
            {
                if (objectTypes[i]==5)
                {
                    bool isOpening=false;
                    m_model.AreaObj.GetOpening(objectIds[i], ref isOpening);
                    if (isOpening) objectTypes[i] = 8;
                }
            }

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
                    case 8: // Opening Object (not api-native)
                        return typeof(Opening);
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

        public static List<Type> GetClassesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes()
                           .Where(t => t.IsClass && t.Namespace == nameSpace)
                           .ToList();
        }


    }
}






