/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2021, the respective contributors. All rights reserved.
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
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Adapter;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Base;

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
            return UpdateObjects(objects as dynamic);
        }

        /***************************************************/

        private bool UpdateObjects(IEnumerable<IBHoMObject> objects)
        {
            return base.IUpdate(objects, null);
        }

        /***************************************************/
    }
}


