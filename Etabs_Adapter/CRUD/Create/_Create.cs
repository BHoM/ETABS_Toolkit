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

using BH.Engine.Adapters.ETABS;
using BH.oM.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Analytical.Elements;
using BH.oM.Analytical.Results;
using BH.oM.Base;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.SurfaceProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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
        protected override bool ICreate<T>(IEnumerable<T> objects, ActionConfig actionConfig = null)
        {
            bool success = true;

            objects = objects.Where(x => x != null); //Filter out any nulls

            if (!objects.Any()) //Return if no objects
                return true;

            if (typeof(BH.oM.Base.IBHoMObject).IsAssignableFrom(typeof(T)))
            {
                success = CreateCollection(objects);
            }
            else
            {
                success = false;
            }

            m_model.View.RefreshView();
            return success;
        }

        /***************************************************/

        private bool CreateCollection<T>(IEnumerable<T> objects) where T : BH.oM.Base.IObject
        {

            bool success = true;

            if (typeof(T) == typeof(Panel))
            {
                List<Panel> panels = objects.Cast<Panel>().ToList();

                List<Diaphragm> diaphragms = panels.Select(x => x.Diaphragm()).Where(x => x != null).ToList();

                this.FullCRUD(diaphragms, PushType.FullPush);
            }

            if (typeof(T) == typeof(oM.Spatial.SettingOut.Level))
            {
                return CreateCollection(objects as IEnumerable<oM.Spatial.SettingOut.Level>);
            }
            else
            {
                foreach (T obj in objects)
                {
                    success &= CreateObject(obj as dynamic);
                }
            }

            if (typeof(T) == typeof(Panel))
            {
                //Force refresh to make sure panel local orientation are set correctly
                ForceRefresh();
            }

            return true;
        }

        /***************************************************/

        private bool CreateObject(IBHoMObject obj)
        {
            Engine.Base.Compute.RecordWarning($"Objects of type {obj.GetType()} are not supported by the ETABSAdapter.");
            return false;
        }

        /***************************************************/

        private bool SetGroup(BHoMObject obj)
        {
            int ret = 0;
            Type type = obj.GetType();
            string objName = "";

            /* 1. CHECK ASSIGNED GROUPS */

            /* Get the list of unique groupNames assigned to the BHoM Bar */
            List<string> groupNames = obj.Tags.ToList();
            /* Get the list of existing group names in the ETABS model */
            int modelNumGroups = 0;
            string[] modelGroupNames = null;
            m_model.GroupDef.GetNameList(ref modelNumGroups, ref modelGroupNames);

            /* Create any groups that do not already exist in the ETABS model */
            foreach (string groupName in groupNames)
            {
                if (!modelGroupNames.Contains(groupName))
                {
                    ret = m_model.GroupDef.SetGroup_1(groupName);
                    if (ret != 0)
                    {
                        Engine.Base.Compute.RecordError("Could not create the Group <" + groupName + "> assigned to the Object. Group not created.");
                        return false;
                    }
                }
            }

            /* 2. ASSIGN OBJECT TO GROUPS */

            if (type == typeof(RigidLink)) {
                /* Get the ETABS names of all the Links */
                List<string> names = ((ETABSId)obj.Fragments[0]).Id as List<string>;
                /* Assign the Links to each group in the list */
                foreach (string name in names) { groupNames.ToList().ForEach(groupName => m_model.LinkObj.SetGroupAssign(name, groupName)); }
            } else {
                /* Get the ETABS name of the Object */
                objName = GetAdapterId<string>(obj);
                /* Assign the Object to each group in the list */
                if (type == typeof(Node)) groupNames.ToList().ForEach(groupName => m_model.PointObj.SetGroupAssign(objName, groupName));
                if (type == typeof(Bar)) groupNames.ToList().ForEach(groupName => m_model.FrameObj.SetGroupAssign(objName, groupName));
                if (type == typeof(Panel) || type == typeof(Opening)) groupNames.ToList().ForEach(groupName => m_model.AreaObj.SetGroupAssign(objName, groupName));

            }

            return true;
        }

        /***************************************************/

    }

}
