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

using System;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.Base.Objects;
using BH.Engine.Structure;

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
        /**** BHoM Adapter Interface                    ****/
        /***************************************************/

        protected void SetupComparers()
        {
            AdapterComparers = new Dictionary<Type, object>
            {
                {typeof(Node), new BH.Engine.Structure.NodeDistanceComparer(3) },
                {typeof(Bar), new BH.Engine.Structure.BarEndNodesDistanceComparer(3) },
                {typeof(ISectionProperty), new NameOrDescriptionComparer() },
                {typeof(IMaterialFragment), new NameOrDescriptionComparer() },
                {typeof(ISurfaceProperty), new NameOrDescriptionComparer() },
                {typeof(BH.oM.Adapters.ETABS.Elements.Diaphragm), new BHoMObjectNameComparer() },
            };
        }
        
        /***************************************************/
    }
}

