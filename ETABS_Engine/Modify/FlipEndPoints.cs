/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2019, the respective contributors. All rights reserved.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.Engine.Structure;
using BH.oM.Geometry;
using BH.oM.Structure.Constraints;

namespace BH.Engine.ETABS
{
    public static partial class Modify
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Bar FlipEndPoints(this Bar bar)
        {
            // Flip the endpoints
            Bar clone = bar.Flip();

            // Flip orientationAngle
            clone.OrientationAngle = -clone.OrientationAngle;

            // Flip Offsets
            if (clone.Offset != null)
            {
                Vector tempV = clone.Offset.Start;
                clone.Offset.Start = clone.Offset.End;
                clone.Offset.End = tempV;

                clone.Offset.Start.X *= -1;
                clone.Offset.End.X *= -1;

                if (!bar.IsVertical())
                {
                    clone.Offset.Start.Y *= -1;
                    clone.Offset.End.Y *= -1;
                }
            }
            // mirror the section 
            // not possible to push to ETABS afterwards if we did
            // warning for asymetric sections?

            // Flip Release
            if (clone.Release != null)
            {
                Constraint6DOF tempC = clone.Release.StartRelease;
                clone.Release.StartRelease = clone.Release.EndRelease;
                clone.Release.EndRelease = tempC;
            }

            return clone;
        }

        /***************************************************/

        public static Bar FlipInsertionPoint(this Bar bar)
        {
            int insertionPoint = (int)bar.InsertionPoint();

            switch (insertionPoint)
            {
                case 1:
                case 4:
                case 7:
                    return bar.SetInsertionPoint((BarInsertionPoint)insertionPoint + 2);
                case 3:
                case 6:
                case 9:
                    return bar.SetInsertionPoint((BarInsertionPoint)insertionPoint - 2);
                default:
                    return bar;
            }
        }

        /***************************************************/

    }
}