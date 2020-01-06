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
using BH.oM.Adapters.ETABS.Elements;
using BH.Engine.Base;

namespace BH.Engine.ETABS
{
    public static partial class Modify
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Bar SetAutoLengthOffset(this Bar bar, bool autoLengthOffset)
        {
            return SetAutoLengthOffset(bar, autoLengthOffset, 1.0);
        }

        /***************************************************/

        public static Bar SetAutoLengthOffset(this Bar bar, bool autoLengthOffset, double rigidZoneFactor)
        {
            if (rigidZoneFactor < 0 || rigidZoneFactor > 1.0)
            {
                rigidZoneFactor = Math.Min(Math.Max(0, rigidZoneFactor), 1);
                Engine.Reflection.Compute.RecordWarning("Rigid zone factor needs to be between 0 and 1. The value has been updated to fit in this interval");
            }
            
            return (Bar)bar.AddFragment(new AutoLengthOffset { AutoOffset = autoLengthOffset, RigidZoneFactor = rigidZoneFactor }, true);
        }

        /***************************************************/
    }
}
