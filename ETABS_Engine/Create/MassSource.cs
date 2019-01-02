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
using BH.oM.Structure.Loads;


namespace BH.Engine.Etabs.Structure
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static MassSource MassSource(bool elementSelfWeight, bool additionalMass, List<Loadcase> loadCases = null, List<double> caseFactors = null)
        {
            List<Tuple<Loadcase, double>> factoredCases = new List<Tuple<Loadcase, double>>();

            if (loadCases != null)
            {
                if (caseFactors == null)
                {
                    Engine.Reflection.Compute.RecordError("If cases are provided, please provide factors as well");
                    return null;
                }

                if (loadCases.Count != caseFactors.Count)
                {
                    Engine.Reflection.Compute.RecordError("Please provide the same number of cases and case factors");
                    return null;
                }

                for (int i = 0; i < loadCases.Count; i++)
                {
                    factoredCases.Add(new Tuple<Loadcase, double>(loadCases[i], caseFactors[i]));
                }
            }
            else if (caseFactors != null)
            {
                Engine.Reflection.Compute.RecordError("If factors are provided, please provide cases as well");
                return null;
            }

            return new MassSource { ElementSelfMass = elementSelfWeight, AdditionalMass = additionalMass, FactoredAdditionalCases = factoredCases };
        }

        /***************************************************/
    }
}
