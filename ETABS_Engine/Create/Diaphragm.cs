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
using System.Text;
using System.Threading.Tasks;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.ETABS;
using BH.Engine.Structure;
using BH.Engine.Geometry;

namespace BH.Engine.ETABS
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Diaphragm Diaphragm(/*List<Panel> panels,*/ string name, DiaphragmType type = DiaphragmType.RigidDiaphragm)
        {
            //List<double> zvals = panels.SelectMany(x => x.AllEdgeCurves().SelectMany(y => y.IControlPoints().Select(z => z.Z))).ToList();

            //if (zvals.Where(x => Math.Abs(zvals.First() - x) > BH.oM.Geometry.Tolerance.Distance).Count() > 0)
            //{
            //    BH.Engine.Reflection.Compute.RecordError("All panels need to be in the same plane");
            //    return null;
            //}

            return new Diaphragm { /*Panels = panels,*/ Name = name, Rigidity = type };
        }

        /***************************************************/
    }
}
