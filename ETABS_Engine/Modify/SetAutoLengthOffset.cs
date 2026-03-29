/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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
using BH.oM.Base.Attributes;
using BH.Engine.Base;
using System.ComponentModel;

namespace BH.Engine.Adapters.ETABS
{
    public static partial class Modify
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Sets the auto length offset on a Bar, using a default rigid zone factor of 1.0.")]
        [Input("bar", "The bar to set the auto length offset on.")]
        [Input("autoLengthOffset", "If true, auto length offset is enabled for the bar.")]
        [Output("bar", "The bar with the auto length offset fragment set.")]
        public static Bar SetAutoLengthOffset(this Bar bar, bool autoLengthOffset)
        {
            return SetAutoLengthOffset(bar, autoLengthOffset, 1.0);
        }

        /***************************************************/

        [Description("Sets the auto length offset on a Bar with a specified rigid zone factor.")]
        [Input("bar", "The bar to set the auto length offset on.")]
        [Input("autoLengthOffset", "If true, auto length offset is enabled for the bar.")]
        [Input("rigidZoneFactor", "The rigid zone factor, clamped to the range [0, 1].")]
        [Output("bar", "The bar with the auto length offset fragment set.")]
        public static Bar SetAutoLengthOffset(this Bar bar, bool autoLengthOffset, double rigidZoneFactor)
        {
            if (rigidZoneFactor < 0 || rigidZoneFactor > 1.0)
            {
                rigidZoneFactor = Math.Min(Math.Max(0, rigidZoneFactor), 1);
                Engine.Base.Compute.RecordWarning("Rigid zone factor needs to be between 0 and 1. The value has been updated to fit in this interval");
            }
            
            return (Bar)bar.AddFragment(new AutoLengthOffset { AutoOffset = autoLengthOffset, RigidZoneFactor = rigidZoneFactor }, true);
        }

        /***************************************************/
    }
}







