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

using BH.Engine.Adapter;
using BH.Engine.Adapters.ETABS;
using BH.oM.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Analytical.Elements;
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.SurfaceProperties;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /**** Adapter override methods                  ****/
        /***************************************************/

        protected override bool IUpdate<T>(IEnumerable<T> objects, ActionConfig actionConfig = null)
        {
            return UpdateObjects(objects as dynamic);
        }

        /***************************************************/

        private bool UpdateObjects(IEnumerable<IBHoMObject> objects)
        {
            return base.IUpdate(objects, null);
        }

        /***************************************************/

#if !(Debug16 || Release16 || Debug17 || Release17)
        private bool UpdateGroup(BHoMObject obj)
        {
            return ResetGroup(obj) && SetGroup(obj);
        }

        /***************************************************/

        private bool ResetGroup(BHoMObject obj)
        {

            Type type = obj.GetType();
            int numGroups = 0;
            string[] groupNames = null;

            if (type == typeof(RigidLink)) {
                /* Get the ETABS names of the Rigid Link */
                List<string> names = GetEtabsNamesForLink((RigidLink)obj);             
                foreach (string name in names)
                {
                    /* Get the names of all groups currently assigned to the link */
                    m_model.LinkObj.GetGroupAssign(name, ref numGroups, ref groupNames);
                    /* Remove the Link from each group in the list */
                    if (groupNames != null)
                        groupNames.ToList().ForEach(groupName => m_model.LinkObj.SetGroupAssign(name, groupName, true));
                }
            } else {
                /* Get the ETABS name of the Object */
                string name = GetAdapterId<string>(obj);

                if (type == typeof(Node)) 
                { 
                    /* Get the names of all groups currently assigned to the node */
                    m_model.PointObj.GetGroupAssign(name, ref numGroups, ref groupNames);
                    /* Remove the Node from each group in the list */
                    groupNames.ToList().ForEach(groupName => m_model.PointObj.SetGroupAssign(name, groupName, true));
                }

                if (type == typeof(Bar))
                {
                    /* Get the names of all groups currently assigned to the object */
                    m_model.FrameObj.GetGroupAssign(name, ref numGroups, ref groupNames);
                    /* Remove the Bar from each group in the list */
                    groupNames.ToList().ForEach(groupName => m_model.FrameObj.SetGroupAssign(name, groupName, true));
                }

                if (type == typeof(Panel) || type == typeof(Opening)) 
                {
                    /* Get the names of all groups currently assigned to the object */
                    m_model.AreaObj.GetGroupAssign(name, ref numGroups, ref groupNames);
                    /* Remove the Panel/Opening from each group in the list */
                    groupNames.ToList().ForEach(groupName => m_model.AreaObj.SetGroupAssign(name, groupName, true));
                }
            }

            return true;
        }

        /***************************************************/
#endif
    }
}
