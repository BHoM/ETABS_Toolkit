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
using System.Collections.Generic;
using System.Linq;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.Constraints;
using BH.Adapter.ETABS;

#if Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif

// ******************************************************
// NOTE
// These Engine methods are improperly put in the Adapter Project
// as a temporary workaround to the different naming of ETABS dlls (2016, 2017).
// Any Engine method that does not require a direct reference to the ETABS dlls
// must be put in the Engine project.
// ******************************************************

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        
        /***************************************************/

        public static LoadNature ToBHoM(this eLoadPatternType loadPatternType)
        {
            switch (loadPatternType)
            {
                case eLoadPatternType.Dead:
                    return LoadNature.Dead;
                case eLoadPatternType.SuperDead:
                    return LoadNature.SuperDead;
                case eLoadPatternType.Live:
                    return LoadNature.Live;
                case eLoadPatternType.Temperature:
                    return LoadNature.Temperature;
                case eLoadPatternType.Braking:
                    return LoadNature.Accidental;
                case eLoadPatternType.Prestress:
                    return LoadNature.Prestress;
                case eLoadPatternType.Wind:
                    return LoadNature.Wind;
                case eLoadPatternType.Quake:
                    return LoadNature.Seismic;
                case eLoadPatternType.Snow:
                    return LoadNature.Snow;
                default:
                    return LoadNature.Other;

            }
        }

        /***************************************************/

        public static MaterialType ToBHoM(this eMatType materialType)
        {
            switch (materialType)
            {
                case eMatType.Steel:
                    return MaterialType.Steel;
                case eMatType.Concrete:
                    return MaterialType.Concrete;
                case eMatType.NoDesign://No material of this type in BHoM !!!
                    return MaterialType.Steel;
                case eMatType.Aluminum:
                    return MaterialType.Aluminium;
                case eMatType.ColdFormed:
                    return MaterialType.Steel;
                case eMatType.Rebar:
                    return MaterialType.Rebar;
                case eMatType.Tendon:
                    return MaterialType.Tendon;
                case eMatType.Masonry://No material of this type in BHoM !!!
                    return MaterialType.Concrete;
                default:
                    return MaterialType.Steel;
            }
        }

        /***************************************************/

        public static String ToBHoM(this eShellType shellType)
        {
            switch (shellType)
            {
                case eShellType.ShellThin:
                    return oM.Adapters.ETABS.ShellType.ShellThin.ToString();
                case eShellType.ShellThick:
                    return oM.Adapters.ETABS.ShellType.ShellThick.ToString();
                case eShellType.Membrane:
                    return oM.Adapters.ETABS.ShellType.Membrane.ToString();
                default:
                    return "None";
            }
        }
    }
}
