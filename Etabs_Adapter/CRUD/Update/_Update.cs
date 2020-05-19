/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
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
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Loads;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Adapter;
using BH.oM.Structure.SurfaceProperties;

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Adapter override methods                  ****/
        /***************************************************/

        protected override bool IUpdate<T>(IEnumerable<T> objects, ActionConfig actionConfig = null)
        {
            if (typeof(T) == typeof(Panel))
            {
                List<Panel> panels = objects.Cast<Panel>().ToList();

                List<Diaphragm> diaphragms = panels.Select(x => x.Diaphragm()).Where(x => x != null).ToList();

                this.FullCRUD(diaphragms, PushType.FullPush);
            }

            if (typeof(T) == typeof(Node))
            {
                return UpdateObjects(objects as IEnumerable<Node>);
            }
            else if (typeof(T) == typeof(Panel))
            {
                return UpdateObjects(objects as IEnumerable<Panel>);
            }
            else if (typeof(T) == typeof(Bar))
            {
                return UpdateObjects(objects as IEnumerable<Bar>);
            }
            else if (typeof(T) == typeof(IMaterialFragment))
            {
                return UpdateObjects(objects as IEnumerable<IMaterialFragment>);
            }
            else if (typeof(T) == typeof(ISectionProperty))
            {
                return UpdateObjects(objects as IEnumerable<ISectionProperty>);
            }
            else if (typeof(T) == typeof(ISurfaceProperty))
            {
                return UpdateObjects(objects as IEnumerable<ISurfaceProperty>);
            }
            else
                return base.IUpdate<T>(objects, actionConfig);
        }

        /***************************************************/
    }
}

