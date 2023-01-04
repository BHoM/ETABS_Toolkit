/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2023, the respective contributors. All rights reserved.
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

using System.Collections.Generic;
using System.Linq;
using BH.oM.Structure.Elements;
using BH.Engine.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Adapter;
using BH.oM.Base;

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

    }
}




