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

using System.Collections.Generic;
using System.Linq;
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System;
using BH.oM.Structure.Loads;

#if Debug17 || Release17
using ETABSv17;
#elif Debug18 || Release18
using ETABSv1;
#else
using ETABS2016;
#endif

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
        /***    Create Methods                           ***/
        /***************************************************/

        public bool CreateObject(Loadcase loadcase)
        {
            eLoadPatternType patternType = LoadNatureToCSI(loadcase.Nature);
            double selfWeight = 0;

            int ret = m_model.LoadPatterns.Add(loadcase.Name, patternType, selfWeight, true);
            loadcase.SetAdapterId(typeof(ETABSId), loadcase.Name);

            return true;
        }

        /***************************************************/

        private bool CreateObject(MassSource massSource)
        {
            bool includeElements = massSource.ElementSelfMass;
            bool includeAddMass = massSource.AdditionalMass;
            bool includeLoads = massSource.FactoredAdditionalCases.Count > 0;

            int count = massSource.FactoredAdditionalCases.Count;
            string[] cases = new string[count];
            double[] factors = new double[count];

            for (int i = 0; i < count; i++)
            {
                cases[i] = massSource.FactoredAdditionalCases[i].Item1.Name;
                factors[i] = massSource.FactoredAdditionalCases[i].Item2;
            }

            if (m_model.PropMaterial.SetMassSource_1(ref includeElements, ref includeAddMass, ref includeLoads, count, ref cases, ref factors) == 0) { }
            else
            {
                CreateElementError("mass source", massSource.Name);
            }

            return true;
        }

        /***************************************************/

        private bool CreateObject(ModalCase modalCase)
        {
            return false;
        }

        /***************************************************/

        private bool CreateObject(LoadCombination loadCombination)
        {
            if (m_model.RespCombo.Add(loadCombination.Name, 0) == 0) //0=case, 1=combo 
            {
                foreach (var factorCase in loadCombination.LoadCases)
                {
                    double factor = factorCase.Item1;
                    Type lcType = factorCase.Item2.GetType();
                    string lcName = factorCase.Item2.Name;// factorCase.Item2.Name;// Number.ToString();
                    eCNameType cTypeName = eCNameType.LoadCase;

                    if (lcType == typeof(Loadcase))
                        cTypeName = eCNameType.LoadCase;
                    else if (lcType == typeof(LoadCombination))
                        cTypeName = eCNameType.LoadCombo;

                    m_model.RespCombo.SetCaseList(loadCombination.Name, ref cTypeName, lcName, factor);
                }
                loadCombination.SetAdapterId(typeof(ETABSId), loadCombination.Name);
            }
            else
            {
                CreateElementError(loadCombination.GetType().ToString(), loadCombination.Name);
            }

            return true;
        }

        /***************************************************/
        /***    Helper Methods                           ***/
        /***************************************************/

        private eLoadPatternType LoadNatureToCSI(LoadNature loadNature)
        {
            eLoadPatternType loadType;
            switch (loadNature)
            {
                case LoadNature.Dead:
                    loadType = eLoadPatternType.Dead;
                    break;
                case LoadNature.SuperDead:
                    loadType = eLoadPatternType.SuperDead;
                    break;
                case LoadNature.Live:
                    loadType = eLoadPatternType.Live;
                    break;
                case LoadNature.Wind:
                    loadType = eLoadPatternType.Dead;
                    break;
                case LoadNature.Seismic:
                    loadType = eLoadPatternType.Quake;
                    break;
                case LoadNature.Temperature:
                    loadType = eLoadPatternType.Temperature;
                    break;
                case LoadNature.Snow:
                    loadType = eLoadPatternType.Snow;
                    break;
                case LoadNature.Accidental:
                    loadType = eLoadPatternType.Braking;
                    break;
                case LoadNature.Prestress:
                    loadType = eLoadPatternType.Prestress;
                    break;
                case LoadNature.Other:
                    loadType = eLoadPatternType.Other;
                    break;
                default:
                    loadType = eLoadPatternType.Other;
                    break;
            }

            return loadType;

        }

        /***************************************************/

    }
}

