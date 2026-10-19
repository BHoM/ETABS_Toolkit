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

using BH.oM.Base.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace BH.Engine.Adapters.PostgreSQL
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Convert ETABS E2K results to CSV format.")]
        [Input("e2kFilePath", "Path to the input ETABS E2K file.", typeof(string))]
        [Input("active", "Execute the file conversion.", typeof(bool))]
        [Output("csvFilePath", "Path to the output CSV file.", typeof(string))]
        public static string FromEtabsE2K(string e2kFilePath, bool active = false)
        {
            if (!active) return "";

            // Need at least an input path.
            if (string.IsNullOrEmpty(e2kFilePath))
            {
                Console.Error.WriteLine("Usage: E2kResultsToCsv <input.e2k> [output.csv]");
                return "";
            }

            // Check if file exists at input path
            if (!File.Exists(e2kFilePath))
            {
                Console.Error.WriteLine("Input file not found: " + e2kFilePath);
                return "";
            }

            // Output path: use arg 2 if given, else "<input>.csv" beside the input.
            string outputCSVFilePath = Path.Combine(
                    Path.GetDirectoryName(Path.GetFullPath(e2kFilePath)) ?? ".",
                    Path.GetFileNameWithoutExtension(e2kFilePath) + ".csv");

            // Build Record Lines by reading the input file
            string[] lines = BuildRecordLines(File.ReadAllLines(e2kFilePath));

            // Identify the type of table 
            string tableName = DetectResultTable(lines);
            if (tableName == null)
            {
                Console.Error.WriteLine("No results TABLE found in the file.");
                return "";
            }

            // Build the CSV Manifest corresponding to the detected type of table
            var csvManifest = BuildCSVManifest(tableName);

            // Build Record Dictionaries from the record lines
            List<Dictionary<string, string>> RecordHashTables = BuildRecordHashTables(lines);

            // Turn record key-value pairs into CSV rows using the manifest's mapping function
            List<string[]> rows = new List<string[]>();

            foreach (var recordKVPair in RecordHashTables)
            {
                string[] row = csvManifest.RowMapper(recordKVPair);
                if (row != null) rows.Add(row);
            }

            // Write the CSV using the manifest's headers and the mapped rows
            WriteCsv(outputCSVFilePath, csvManifest.Headers, rows);

            // Report what we detected and produced.
            Console.WriteLine("Detected table: \"" + tableName + "\"  ->  wrote " + rows.Count + " rows to " + outputCSVFilePath);
            return outputCSVFilePath;
        }

        /***************************************************/
    }
}


