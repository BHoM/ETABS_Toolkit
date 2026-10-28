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
        /**** Private Methods                           ****/
        /***************************************************/

        //  DETECT RESULT TABLE
        //  Method returning the name of the first TABLE that is not "PROGRAM CONTROL"
        private static string DetectResultTable(string[] lines)
        {
            // Matches:  TABLE:  "Some Name"   and captures the quoted name.
            Regex tableLine = new Regex("^\\s*TABLE:\\s*\"([^\"]+)\"");
            foreach (string line in lines)
            {
                // Compare the current loop line to the regex pattern to see if it matches a table line.
                Match m = tableLine.Match(line);
                // Take the first table whose name isn't the program-control metadata block.
                if (m.Success && !m.Groups[1].Value.Equals("PROGRAM CONTROL", StringComparison.OrdinalIgnoreCase))
                    return m.Groups[1].Value;
            }
            // No results table found.
            return null;
        }

        // BUILD LOGICAL LINES
        // Method rejoining records split across multiple lines using trailing " _"
        private static string[] BuildRecordLines(string[] rawLines)
        {
            // Rebuilt record lines
            List<string> recordLines = new List<string>();
            // String builder to concatenate continued lines
            StringBuilder sb = new StringBuilder();
            // Regex to detect a line ending with " _" (space underscore space)
            Regex cont = new Regex(@"\s_\s*$");

            // Loop through each raw line and check for continuation
            foreach (string raw in rawLines)
            {
                // If the line ends with " _", remove " _" and keep buffering.
                if (cont.IsMatch(raw))
                    sb.Append(cont.Replace(raw, " "));
                /* ...otherwise, if the line doesn't end with " _" and the buffer is not empty,
                   append the line to the buffer, add the whole line to the recordLines list,
                   and reset the buffer. */
                else
                {
                    // Final piece: append, emit the whole recordLines line, reset the buffer.
                    sb.Append(raw);
                    recordLines.Add(sb.ToString());
                    sb.Clear();
                }
            }
            // If the last line ends with " _", the buffer will not be empty, so we need to add it to the recordLines list.
            if (sb.Length > 0) recordLines.Add(sb.ToString());
            // Return the rebuilt record lines as an array.
            return recordLines.ToArray();
        }

        //  BUILD RECORD HASH TABLES
        //  Turn each record string line made from textual key-value pairs into a corresponding hash table data structure
        //  - i.e. a hash table of normalized key-value pairs.
        private static List<Dictionary<string, string>> BuildRecordHashTables(string[] recordLines)
        {
            // Initialize List Data Structure to hold the dictionaries for each record line.
            List<Dictionary<string, string>> recordsList = new List<Dictionary<string, string>>();

            foreach (string raw in recordLines)
            {
                // Trim whitespace from the line.
                string line = raw.Trim();
                // If the line is blank or starts with a comment character / table header, skip it.
                if ((line.Length == 0) || (line.StartsWith("$")) ||
                    (line.StartsWith("TABLE:", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                // Split into tokens, keeping quoted spans intact
                List<string> tokens = TokenizeRespectingQuotes(line);

                // Initialize a new hash table to hold the key-value pairs from the record line.
                Dictionary<string, string> records = new Dictionary<string, string>();
                /* Loop through each token and split it into key-value pairs, normalizing the keys
                   and stripping quotes from the values. */
                foreach (string token in tokens)
                {
                    // If the token is blank, skip it.
                    if (token.IndexOf('=') <= 0) continue;
                    int splitIndex = token.IndexOf('=');
                    // Split the token into key and value.
                    // Normalize the key, and strip quotes from the value.
                    string key = NormalizeKey(token.Substring(0, splitIndex));
                    string val = StripQuotes(token.Substring(splitIndex + 1).Trim());
                    // Add the key-value pair to the hash table ONLY IF the key is not already present
                    if (!records.ContainsKey(key)) records[key] = val;
                }
                // If the hash table is not empty, add it to the list of records.
                if (records.Count > 0) recordsList.Add(records);
            }
            // Return the list of dictionaries representing the records.
            return recordsList;
        }

        //  TOKENIZE RESPECTING QUOTES
        //  Split record lines on whitespace, but treat double-quoted spans as a single item
        //  so that internal white spaces don't split a single value/key.
        private static List<string> TokenizeRespectingQuotes(string recordLine)
        {
            // Initialize a list to hold the tokens
            var tokens = new List<string>();
            // Initialize the StringBuilder to accumulate characters for the current token.
            var sb = new StringBuilder();
            // Initialize a boolean to track whether we are inside quotes.
            bool inQuotes = false;

            // Loop through each character in the record line
            foreach (char c in recordLine)
            {
                // If the character is a double quote, toggle the inQuotes state and append the quote to the current token.
                if (c == '"') { inQuotes = !inQuotes; sb.Append(c); }
                // If the character is whitespace and we are not inside quotes, it indicates the end of a token....
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    // ...therefore, if the StringBuilder has accumulated characters,
                    // add the current token to the list and clear the StringBuilder for the next token.
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                }
                // ...otherwise, append the character to the current token.
                else sb.Append(c);
            }
            // After the loop, if there are any remaining characters in the StringBuilder, add them as the last token.
            if (sb.Length > 0) tokens.Add(sb.ToString());
            // Return the list of tokens.
            return tokens;
        }

        //  NORMALIZE KEY
        //  Method re-formatting the field name to be lowercase and contain letters/digits only.
        //  e.g.: "Unique Name" -> "uniquename", "Output Case" -> "outputcase".
        private static string NormalizeKey(string s)
        {
            // If the input string is null, return an empty string.
            if (s == null) return "";
            // Initialize a StringBuilder to accumulate the normalized characters.
            StringBuilder sb = new StringBuilder();
            // Modify the input string by trimming whitespace and converting to lowercase invariant.
            string smod = s.Trim().ToLowerInvariant();
            // Loop through each character in the modified string and append only letters and digits to the StringBuilder.
            foreach (char c in smod)
                // Drop spaces, quotes, underscores and punctuation
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            // Return the normalized key string.
            return sb.ToString();
        }

        //  STRIP QUOTES
        //  Method removing the pair of double quotes surrounding a string
        private static string StripQuotes(string s)
        {
            // If the string has >2 characters and has quotes at both ends, remove the quotes
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                // Return the substring without quotes
                return s.Substring(1, s.Length - 2);
            // Otherwise, return the string as it is
            return s;
        }

        //  WRITECSV
        //  Method writing header row + all data rows
        private static void WriteCsv(string path, string[] headers, List<string[]> rows)
        {
            // Initialize a StringBuilder to concatenate the content of the CSV file.
            StringBuilder sb = new StringBuilder();
            // Build the Header line joining headers with commas
            sb.AppendLine(string.Join(",", headers.Select(CsvEscape)));
            // Build the Data lines joining fields with commas
            foreach (string[] row in rows)
                sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
            // Save as UTF-8 without a byte-order mark (best for spreadsheet/import compatibility).
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        // CSVESCAPE
        // Add quotes around a field if it contains a comma, quote, or newline, and escape any internal quotes by doubling them.
        private static string CsvEscape(string field)
        {
            // If the field is null, treat it as an empty string.
            if (field == null) field = "";
            // Replace any comma with an underscore
            field = field.Replace(',', '_');
            // If the field contains a comma, quote, or newline, wrap it in quotes and remove any internal quotes by doubling them.
            if (field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            // Return the field as it is if no special characters are present.
            return field;
        }

        // GETVALUE
        // Method returning the value for a given key in a record hash table.
        // If the key is not present, an empty string is returned instead of throwing an exception.
        private static string GetValue(Dictionary<string, string> record, string key)
        {
            string value;
            return record.TryGetValue(key, out value) ? value : "";
        }

    }
}


