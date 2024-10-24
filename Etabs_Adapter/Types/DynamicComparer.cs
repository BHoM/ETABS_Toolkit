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


using BH.oM.Adapters.ETABS;
using BH.oM.Base;
using BH.oM.Spatial.SettingOut;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.SurfaceProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BH.Adapter.ETABS.Types
{
        public class DynamicComparer : IEqualityComparer<IBHoMObject>
        {

            // Use of STREAMS and REFLECTIONS

            // 1. Equality based on ETABS obj Id or Name
            public bool Equals(IBHoMObject obj1, IBHoMObject obj2)
            {
                if (obj1 == null || obj2 == null) return false;
                if (obj1.GetType() != obj2.GetType()) return false;


                Type objType = obj1.GetType();

                // For physical objects, use Id as it is never null
                if (objType == typeof(Node) || objType == typeof(Bar) || objType == typeof(Panel) || objType == typeof(RigidLink) || objType == typeof(FEMesh))
                    return GetEtabsId(obj1).Id.ToString() == GetEtabsId(obj2).Id.ToString();
                // For all other items, use Name as it is never null
                if (objType == typeof(ISectionProperty) || objType == typeof(IMaterialFragment) || objType == typeof(ISurfaceProperty) ||
                    objType == typeof(Loadcase) || objType == typeof(LoadCombination) || objType == typeof(ILoad) || objType == typeof(LinkConstraint) ||
                    objType == typeof(Level) || objType == typeof(Grid))
                    return obj1.Name.ToString() == obj2.Name.ToString();

                return false;

            }

            // 2. HashCode based on Hash function of ETABS obj Id+Label or Type+Name
            public int GetHashCode(IBHoMObject obj)
            {
                Type objType = obj.GetType();

                // For physical objects, use Id and Label as they are never null
                if (objType == typeof(Node) || objType == typeof(Bar) || objType == typeof(Panel) || objType == typeof(RigidLink) || objType == typeof(FEMesh))
                    return (GetEtabsId(obj).Id.ToString() + GetEtabsId(obj).Label.ToString()).GetHashCode();
                // For all other items, use Name as Id/Label can be null
                else if (objType == typeof(ISectionProperty) || objType == typeof(IMaterialFragment) || objType == typeof(ISurfaceProperty) ||
                    objType == typeof(Loadcase) || objType == typeof(LoadCombination) || objType == typeof(ILoad) || objType == typeof(LinkConstraint) ||
                    objType == typeof(Level) || objType == typeof(Grid))
                    return (obj.GetType().ToString() + obj.Name.ToString()).GetHashCode();

                return 0;

            }

            // 3. Get ETABS Id using REFLECTION - **REFLECTION**
            private ETABSId GetEtabsId(IBHoMObject obj)
            {
                // 1. Get the object Type
                Type objsType = obj.GetType();
                // 2. Get the Property Fragments - via REFLECTION
                PropertyInfo fragmentsProperty = objsType.GetProperty("Fragments");
                // 3. Downcast the Fragments Property to FragmentSet Class - via REFLECTION
                FragmentSet fragments = (FragmentSet)fragmentsProperty.GetValue(obj);
                // 4. Get the ETABSId object contained in the FragmentSet - via STREAMS
                ETABSId etabsId = (ETABSId)(fragments.ToList().Find(frag => frag.GetType() == typeof(ETABSId)));
                // 5. Return the Etabs Id of the object
                return etabsId;
            }


        }
}
