/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2024, the respective contributors. All rights reserved.
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
using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.Constraints;
using BH.oM.Geometry;
using BH.Engine.Geometry;

namespace BH.Adapter.ETABS
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Vector FromCSILocalX(Vector normal, double orientationAngle)
        {
            Vector locYref;

            if (normal.IsEtabsVertical())
            {
                //Vector is paralell to z-axis
                locYref = Vector.YAxis;
            }
            else
            {
                //Vector is not paralell to z-axis
                locYref = Vector.ZAxis.Project(new Plane { Normal = normal });
            }

            Vector localXref = locYref.CrossProduct(normal);

            return localXref.Rotate(orientationAngle / 180 * Math.PI, normal);
        }

        /***************************************************/

        private static bool IsEtabsVertical(this Vector vector)
        {
            return Math.Sin(vector.Angle(Vector.ZAxis)) < 1e-3;
        }

        /***************************************************/
    }
}





