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

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using BH.oM.Structure.Loads;
using BH.Engine;
#if Debug17 || Release17
using ETABSv17;
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter 
#else
    public partial class ETABS2016Adapter
#endif
    {
        /***************************************************/
        /****           Adapter Methods                 ****/
        /***************************************************/

        public override bool Execute(string command, Dictionary<string, object> parameters = null, Dictionary<string, object> config = null)
        {
            string commandUpper = command.ToUpper();
            if (commandUpper == "ANALYSE" || commandUpper == "RUN")
              {
                IList cases = null;
                //string[] caseStringAlt =
                //{
                //    "Cases",
                //    "CASES",
                //    "cases",
                //    "LoadCases",
                //    "LOADCASES",
                //    "loadcases",
                //    "Loadcases",
                //    "Load Cases",
                //    "LOAD CASES",
                //    "load cases",
                //    "Load cases",
                //    "Load_Cases",
                //    "LOAD_CASES",
                //    "load_cases",
                //    "Load_cases"
                //};
                //foreach (string str in caseStringAlt)
                //{
                //    object obj;
                //    if (parameters.TryGetValue(str, out obj))
                //    {
                //        cases = obj as IList;
                //        break;
                //    }
                //}
                return Analyse(cases);
            }
            else if (commandUpper == "DELETE" )
            {
                //m_model.File.Save(m_model.GetModelFilepath().ToString());
                //m_model.File.NewBlank();

                //m_model.SetPresentUnits(eUnits.N_m_C);
                m_model.SelectObj.All(false);
                m_model.SetModelIsLocked(false);
                m_model.FrameObj.Delete("asdf", eItemType.SelectedObjects);
                m_model.AreaObj.Delete("asdf", eItemType.SelectedObjects);
                m_model.PointObj.DeleteSpecialPoint("asdf", eItemType.SelectedObjects);
                m_model.View.RefreshView();
            
                return true;
            }
            else
                return false;
        }

        /***************************************************/

        public bool Analyse(IList cases = null)
        {
            try
            {
                if (m_model.File.Save(m_model.GetModelFilepath().ToString()) == 0)
                {
                    m_model.Analyze.RunAnalysis();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                Engine.Reflection.Compute.RecordWarning("ETABS adapter needs a filepath or no analysis can be performed");
                return false;
            }

        }

        /***************************************************/
    }
}
