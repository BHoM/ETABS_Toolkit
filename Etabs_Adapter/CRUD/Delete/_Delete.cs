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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Adapter;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Loads;
using BH.oM.Structure.Constraints;
using BH.oM.Base;
using BH.oM.Data.Requests;
using System.Collections;

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
        /**** Public Methods                            ****/
        /***************************************************/

        protected override int IDelete(Type type, IEnumerable<object> ids, ActionConfig actionConfig = null)
        {
            List<string> listIds = new List<string>();
            if (ids != null)
                listIds = ids.Cast<string>().ToList();

            int count = listIds.Count;
            int controlCount = count;

            if (type == typeof(Node))
                foreach (string id in listIds)
                    count -= m_model.PointObj.DeleteSpecialPoint(id);
            else if (type == typeof(Bar))
                foreach (string id in listIds)
                    count -= m_model.FrameObj.Delete(id);
            else if (type == typeof(ISectionProperty) || type.GetInterfaces().Contains(typeof(ISectionProperty)))
                foreach (string id in listIds)
                    count -= m_model.PropFrame.Delete(id);
            else if (type == typeof(IMaterialFragment))
                foreach (string id in listIds)
                    count -= m_model.PropMaterial.Delete(id);
            else if (type == typeof(Panel))
                foreach (string id in listIds)
                    count -= m_model.AreaObj.Delete(id);
            else if (type == typeof(ISurfaceProperty))
                foreach (string id in listIds)
                    count -= m_model.PropArea.Delete(id);
            else if (type == typeof(LoadCombination))
                foreach (string id in listIds)
                    count -= m_model.LoadPatterns.Delete(id);
            else if (type == typeof(Loadcase))
                foreach (string id in listIds)
                    count -= m_model.LoadCases.Delete(id);
            else if (type == typeof(ILoad) || type.GetInterfaces().Contains(typeof(ILoad)))
                Engine.Base.Compute.RecordError("Loads do not have ids in ETABS, try deleting Loadcases, LoadCombinations or overwriting the old loads with `ReplaceLoads`.");
            else if (type == typeof(RigidLink))
                foreach (string id in listIds)
                    count -= m_model.LinkObj.Delete(id);
            else if (type == typeof(LinkConstraint))
                foreach (string id in listIds)
                    count -= m_model.PropLink.Delete(id);
            else if (type == typeof(oM.Spatial.SettingOut.Level))
                Engine.Base.Compute.RecordError("Can't delete levels in ETABS.");
            else if (type == typeof(oM.Spatial.SettingOut.Grid))
                Engine.Base.Compute.RecordError("Can't delete grids in ETABS.");
            else if (type == typeof(FEMesh))
                foreach (string id in listIds)
                    count -= m_model.AreaObj.Delete(id);

            if (count != controlCount)
            {
                Engine.Base.Compute.RecordWarning("Some of the requested delete operations failed.");
            }

            return count;
        }

        /***************************************************/

        protected override int Delete(IRequest request, ActionConfig actionConfig = null)
        {
            if (request is SelectionRequest)
            {
                int count = 0;

                foreach (KeyValuePair<Type, List<string>> keyVal in SelectedElements())
                {
                    count += IDelete(keyVal.Key, keyVal.Value, actionConfig);
                }

                return count;
            }

            // Temporary fix until BHoM_Adpter works
            if (request is FilterRequest)
            {
                List<string> objectIds = null;
                object idObject;
                if ((request as FilterRequest).Equalities.TryGetValue("ObjectIds", out idObject) && idObject is List<string>)
                    objectIds = idObject as List<string>;

                // Call the Basic Method Read() to get the objects based on the Ids
                return IDelete((request as FilterRequest).Type, objectIds, actionConfig);
            }

            return base.Delete(request, actionConfig);
        }

        /***************************************************/
    }
}


