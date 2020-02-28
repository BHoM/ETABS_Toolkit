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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Architecture.Elements;
using BH.oM.Geometry.SettingOut;

namespace BH.Engine.ETABS
{
    public static partial class Modify
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static List<oM.Geometry.SettingOut.Level> UpgradeVersion(this List<BH.oM.Architecture.Elements.Level> levels)
        {
            List<oM.Geometry.SettingOut.Level> upgradedLevels = new List<oM.Geometry.SettingOut.Level>();

            foreach (BH.oM.Architecture.Elements.Level level in levels)
                upgradedLevels.Add(level.UpgradeVersion());

            return upgradedLevels;
        }

        /***************************************************/

        public static oM.Geometry.SettingOut.Level UpgradeVersion(this BH.oM.Architecture.Elements.Level level)
        {
            return new oM.Geometry.SettingOut.Level {
                Name = level.Name,
                Elevation = level.Elevation,
                CustomData = level.CustomData,
                Fragments = level.Fragments };
        }

        /***************************************************/
    }
}

