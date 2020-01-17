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
using BH.oM.Adapter;
using BH.oM.Reflection;
using BH.oM.Adapter.Commands;
using BH.oM.Structure.Loads;

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
        /**** IAdapter Interface                        ****/
        /***************************************************/

        public override Output<List<object>, bool> Execute(IExecuteCommand command, ActionConfig actionConfig = null)
        {
            var output = new Output<List<object>, bool>() { Item1 = null, Item2 = false };

            output.Item2 = RunCommand(command as dynamic);

            return output;
        }

        /***************************************************/

        public bool RunCommand(NewModel command)
        {
            bool success = m_model.File.NewBlank() == 0;
            success &= m_model.SetPresentUnits(eUnits.N_m_C) == 0;
            return success;
        }

        /***************************************************/

        public bool RunCommand(Save command)
        {
            return m_model.File.Save() == 0;
        }

        /***************************************************/

        public bool RunCommand(SaveAs command)
        {
            return m_model.File.Save("@" + command.FileName) == 0;
        }

        /***************************************************/

        public bool RunCommand(Open command)
        {
            bool success = m_model.File.OpenFile("@" + command.FileName) == 0;
            success &= m_model.SetPresentUnits(eUnits.N_m_C) == 0;
            return success;
        }

        /***************************************************/

        public bool RunCommand(Analyse command)
        {
            bool success = m_model.Analyze.SetRunCaseFlag("", true, true) == 0;

            if (!success)
            {
                Engine.Reflection.Compute.RecordWarning("Failed to set up cases to run. Model has not been analysed");
                return false;
            }

            success &= m_model.Analyze.RunAnalysis() == 0;
            return success;
        }

        /***************************************************/

        public bool RunCommand(AnalyseLoadCases command)
        {
            bool success;
            var cases = command.LoadCases;

            if (cases == null)
            {
                Engine.Reflection.Compute.RecordNote("No cases provided, all cases will be run");
                success = m_model.Analyze.SetRunCaseFlag("", true, true) == 0;
            }
            else
            {
                //Unselect all cases
                success = m_model.Analyze.SetRunCaseFlag("", false, true) == 0;
                
                //Select provided cases
                foreach (object item in cases)
                {
                    string name;
                    if (item == null)
                        continue;
                    if (item is string)
                        name = item as string;
                    else if (item is ICase)
                        name = (item as ICase).Name;
                    else
                    {
                        Engine.Reflection.Compute.RecordWarning("Can not set up cases for running of type " + item.GetType().Name + ". Item " + item.ToString() + " will be ignored. Please provide case names or BHoM cases to be run");
                        continue;
                    }

                    bool caseSuccess = m_model.Analyze.SetRunCaseFlag("", false, true) == 0;
                    success &= caseSuccess;

                    if (!caseSuccess)
                    {
                        Engine.Reflection.Compute.RecordWarning("Failed to set case " + name + "for running. Please check that the case exists in the model");
                    }
                }

            }

            success &= m_model.Analyze.RunAnalysis() == 0;
            return success;
        }

        /***************************************************/

        public bool RunCommand(ClearResults command)
        {
            return m_model.Analyze.DeleteResults("", true) == 0;
        }

        /***************************************************/

        public bool RunCommand(IExecuteCommand command)
        {
            Engine.Reflection.Compute.RecordWarning("Etabs can not handle this command");
            return false;
        }

        /***************************************************/
    }
}

