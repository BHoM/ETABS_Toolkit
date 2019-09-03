/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
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
#if (Debug2017)
using ETABSv17;
#else
using ETABS2016;
#endif
using BH.Engine.ETABS;
using BH.oM.Geometry;
using BH.Engine.Geometry;
using BH.Engine.Reflection;
using BH.oM.Architecture.Elements;
using BH.oM.Adapters.ETABS.Elements;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        /***************************************************/
        /*** Private methods - Read                      ***/
        /***************************************************/

        protected override IEnumerable<IBHoMObject> Read(Type type, IList ids)
        {
            if (type == typeof(Node))
                return ReadNode(ids as dynamic);
            else if (type == typeof(Bar))
                return ReadBar(ids as dynamic);
            else if (type == typeof(ISectionProperty) || type.GetInterfaces().Contains(typeof(ISectionProperty)))
                return ReadSectionProperty(ids as dynamic);
            else if (type == typeof(IMaterialFragment))
                return ReadMaterial(ids as dynamic);
            else if (type == typeof(Panel))
                return ReadPanel(ids as dynamic);
            else if (type == typeof(ISurfaceProperty))
                return ReadSurfaceProperty(ids as dynamic);
            else if (type == typeof(LoadCombination))
                return ReadLoadCombination(ids as dynamic);
            else if (type == typeof(Loadcase))
                return ReadLoadcase(ids as dynamic);
            else if (type == typeof(ILoad) || type.GetInterfaces().Contains(typeof(ILoad)))
                return ReadLoad(type, ids as dynamic);
            else if (type == typeof(RigidLink))
                return ReadRigidLink(ids as dynamic);
            else if (type == typeof(LinkConstraint))
                return ReadLinkConstraints(ids as dynamic);
            else if (type == typeof(Level))
                return ReadLevel(ids as dynamic);
            else if (type == typeof(FEMesh))
                return ReadMesh(ids as dynamic);

            return new List<IBHoMObject>();//<--- returning null will throw error in replace method of BHOM_Adapter line 34: can't do typeof(null) - returning null does seem the most sensible to return though
        }

        /***************************************************/
    }
}
