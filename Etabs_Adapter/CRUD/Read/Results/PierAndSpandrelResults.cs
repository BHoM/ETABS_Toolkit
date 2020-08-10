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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Results;
using BH.oM.Analytical.Results;
#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif
using BH.oM.Structure.Requests;
using BH.oM.Adapter;
using BH.oM.Adapters.ETABS.Results;
using BH.oM.Adapters.ETABS.Requests;

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#elif Debug18 || Release18
   public partial class ETABS18Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public method - Read override             ****/
        /***************************************************/

        public IEnumerable<IResult> ReadResults(PierAndSpandrelForceRequest request, ActionConfig actionConfig = null)
        {
            CheckAndSetUpCases(request);

            switch (request.ResultType)
            {
                case PierAndSpandrelResultType.PierForce:
                    return GetPierForce(request.ObjectIds);
                default:
                    Engine.Reflection.Compute.RecordError("Result extraction of type " + request.ResultType + " is not yet supported");
                    return new List<IResult>();
            }
        }

        /***************************************************/
        /**** Private method - Extraction methods       ****/
        /***************************************************/

        private List<PierForce> GetPierForce(IList ids = null)
        {
            List<PierForce> pierForces = new List<PierForce>();

            string[] loadcaseNames = null;

            int numberResults = 0;
            string[] storyName = null;
            string[] pierName = null;
            string[] location = null;

            double[] p = null;
            double[] v2 = null;
            double[] v3 = null;
            double[] t = null;
            double[] m2 = null;
            double[] m3 = null;

            int ret = m_model.Results.PierForce(ref numberResults, ref storyName, ref pierName, ref loadcaseNames, ref location, ref p, ref v2, ref v3, ref t, ref m2, ref m3);
            if (ret == 0)
            {
                for (int j = 0; j < numberResults; j++)
                {
                    int position = 0;
                    if (location[j].ToUpper().Contains("BOTTOM"))
                    {
                        position = 1;
                    }

                    PierForce pf = new PierForce(pierName[j], loadcaseNames[j], storyName[j], 0, 0, position, 2, p[j], v2[j], v3[j], t[j], m2[j], m3[j]);
                    pierForces.Add(pf);
                }

            }
            return pierForces;
        }


        /***************************************************/

    }
}


