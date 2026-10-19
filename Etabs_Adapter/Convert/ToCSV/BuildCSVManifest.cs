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

        private static (string[] Headers, Func<Dictionary<string, string>, string[]> RowMapper) BuildCSVManifest(string tableName)
        {
            // Turn the table name into uppercase for case-insensitive comparison.
            tableName = tableName.ToUpperInvariant();

            // ---- ELEMENT FORCES - BEAMS / COLUMNS / BRACES  ->  BHoM BarForce ----
            if (tableName.Contains("ELEMENT") && tableName.Contains("FORCES") &&
               (tableName.Contains("BEAM") || tableName.Contains("COLUMN") ||
                tableName.Contains("BRACE") || tableName.Contains("FRAME")))
                return (
                    Headers: new[] { "fx", "fy", "fz", "mx", "my", "mz",
                                  "object_id", "resultcase", "modenumber", "timestep", "position", "divisions" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a force and a joint id, skip it...
                        if (!row.ContainsKey("p") || !row.ContainsKey("uniquename")) return (string[])null;
                        // ETABS subdivides frame elements internally and outputs results at each sub-element
                        // boundary. When ElemStation != 0 the row sits at an intermediate sub-element
                        // point whose absolute Station value coincides with the end of the previous
                        // sub-element. Without an offset, two rows would share the same (object_id,
                        // position) and cause a duplicate-key conflict in the database. Subtracting
                        // 0.01 makes the position unique while keeping it negligibly close to the
                        // true station.
                        string stationVal = GetValue(row, "station");
                        string elemStationVal = GetValue(row, "elemstation");
                        double elemStation;
                        if (double.TryParse(elemStationVal, out elemStation) && elemStation != 0)
                        {
                            double station;
                            if (double.TryParse(stationVal, out station))
                                stationVal = (station - 0.01).ToString();
                        }
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Forces: fx, fy, fz  <- P, V2, V3
                            GetValue(row, "p"), GetValue(row, "v2"), GetValue(row, "v3"),
                            // Moments: mx, my, mz  <- T, M2, M3
                            GetValue(row, "t"), GetValue(row, "m2"), GetValue(row, "m3"),
                            // Object ID
                            GetValue(row, "uniquename"),
                            // Result Case
                            GetValue(row, "outputcase"),
                            // ModeNumberow and Timestep (constants)
                            "0", "0",
                            // Position and Divisions (constant)
                            stationVal, "3" };
                    }
                );

            // ---- JOINT DISPLACEMENTS  ->  BHoM NodeDisplacement ----
            if (tableName.Contains("JOINT") && tableName.Contains("DISPLACEMENT"))
                return (
                    Headers: new[] { "ux", "uy", "uz", "rx", "ry", "rz",
                                  "object_id", "resultcase", "modenumber", "timestep", "orientation" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a translation and a joint id, skip it...
                        if (!row.ContainsKey("ux") || !row.ContainsKey("uniquename")) return (string[])null;
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Translations: ux, uy, uz
                            GetValue(row, "ux"), GetValue(row, "uy"), GetValue(row, "uz"),
                            // Rotations: rx, ry, rz
                            GetValue(row, "rx"), GetValue(row, "ry"), GetValue(row, "rz"),
                            // Object ID and Result Case
                            GetValue(row, "uniquename"), GetValue(row, "outputcase"),
                            // ModeNumberow, Timestep (constants) and Orientation (constants)
                            "0", "0",  null };
                    }
                );

            // ---- JOINT REACTIONS  ->  BHoM NodeReaction ----
            if (tableName.Contains("JOINT") && tableName.Contains("REACTION"))
                return (
                    Headers: new[] { "fx", "fy", "fz", "mx", "my", "mz",
                                  "object_id", "resultcase", "modenumber", "timestep", "orientation" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a force and a joint id, skip it...
                        if (!row.ContainsKey("fx") || !row.ContainsKey("uniquename")) return (string[])null;
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Forces: fx, fy, fz
                            GetValue(row, "fx"), GetValue(row, "fy"), GetValue(row, "fz"),
                            // Moments: mx, my, mz
                            GetValue(row, "mx"), GetValue(row, "my"), GetValue(row, "mz"),
                            // Object ID and Result Case
                            GetValue(row, "uniquename"), GetValue(row, "outputcase"),
                            // ModeNumberow, Timestep and Orientation (constants)
                            "0", "0",  null };
                    }
                );

            // ---- ELEMENT FORCES - AREA SHELLS  ->  BHoM MeshForce ----
            if (tableName.Contains("ELEMENT") && tableName.Contains("FORCES") &&
               (tableName.Contains("SHELL") || tableName.Contains("AREA")))
                return (
                    Headers: new[] { "object_id", "node_id", "resultcase", "modenumber", "timestep",
                                  "nxx", "nyy", "nxy", "mxx", "myy", "mxy", "vx", "vy" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a force and a joint id, skip it...
                        if (!row.ContainsKey("f11") || !row.ContainsKey("uniquename")) return (string[])null;
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Object ID and Node ID (per-joint result)
                            GetValue(row, "uniquename"), GetValue(row, "joint"),
                            // Result Case, ModeNumberow, Timestep (constants)
                            GetValue(row, "outputcase"), "0", "0",            
                            // Membrane Forces: f11, f22, f12
                            GetValue(row, "f11"), GetValue(row, "f22"), GetValue(row, "f12"),
                            // Bending Moments: m11, m22, m12
                            GetValue(row, "m11"), GetValue(row, "m22"), GetValue(row, "m12"),
                            // Transverse Shears: v13, v23
                            GetValue(row, "v13"), GetValue(row, "v23") };
                    }
                );

            // ---- BASE REACTIONS  ->  BHoM GlobalReactions (no element id; global) ----
            if (tableName.Contains("BASE") && tableName.Contains("REACTION"))
                return (
                    Headers: new[] { "fx", "fy", "fz", "mx", "my", "mz", "x", "y", "z",
                                  "resultcase", "modenumber", "timestep" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a force, skip it...
                        if (!row.ContainsKey("fx")) return (string[])null;
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Reaction Forces: fx, fy, fz
                            GetValue(row, "fx"), GetValue(row, "fy"), GetValue(row, "fz"),
                            // Reaction Moments: mx, my, mz
                            GetValue(row, "mx"), GetValue(row, "my"), GetValue(row, "mz"),
                            // Reaction Point: x, y, z
                            GetValue(row, "x"), GetValue(row, "y"), GetValue(row, "z"),
                            // Result Case, ModeNumberow, Timestep (constants)
                            GetValue(row, "outputcase"), "0", "0" };
                    }
                );

            // If the table name matches none of the above, log error, return error message in the console and return null outputs.
            BH.Engine.Base.Compute.RecordError("Unrecognized ETABS results table: \"" + tableName + "\".");
            Console.WriteLine("Unrecognized ETABS results table: \"" + tableName + "\".");
            return (null, null);
        }


        /***************************************************/

        private static (string[] Headers, Func<Dictionary<string, string>, string[]> RowMapper) BuildCSVManifestForSap2000(string tableName)
        {
            // Turn the table name into uppercase for case-insensitive comparison.
            tableName = tableName.ToUpperInvariant();

            // ---- ELEMENT FORCES - BEAMS / COLUMNS / BRACES  ->  BHoM BarForce ----
            if (tableName.Contains("ELEMENT") && tableName.Contains("FORCES") && tableName.Contains("FRAME"))
                return (
                    Headers: new[] { "fx", "fy", "fz", "mx", "my", "mz",
                                  "object_id", "resultcase", "modenumber", "timestep", "position", "divisions" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a force and a joint id, skip it...
                        if (!row.ContainsKey("p") || !row.ContainsKey("frame")) return (string[])null;
                        // SAP2000 subdivides frame elements internally and outputs results at each
                        // sub-element boundary. When ElemStation != 0 the row sits at an intermediate
                        // sub-element point whose absolute Station value coincides with the end of the
                        // previous sub-element. Without an offset, two rows would share the same
                        // (object_id, position) and cause a duplicate-key conflict in the database.
                        // Subtracting 0.01 makes the position unique while keeping it negligibly
                        // close to the true station.
                        string stationVal = GetValue(row, "station");
                        string elemStationVal = GetValue(row, "elemstation");
                        double elemStation;
                        if (double.TryParse(elemStationVal, out elemStation) && elemStation != 0)
                        {
                            double station;
                            if (double.TryParse(stationVal, out station))
                                stationVal = (station - 0.01).ToString();
                        }
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Forces: fx, fy, fz  <- P, V2, V3
                            GetValue(row, "p"), GetValue(row, "v2"), GetValue(row, "v3"),
                            // Moments: mx, my, mz  <- T, M2, M3
                            GetValue(row, "t"), GetValue(row, "m2"), GetValue(row, "m3"),
                            // Object ID
                            GetValue(row, "frame"),
                            // Result Case
                            GetValue(row, "outputcase"),
                            // ModeNumberow and Timestep (constants)
                            "0", "0",
                            // Position and Divisions (constant)
                            stationVal, "3" };
                    }
                );

            // ---- JOINT DISPLACEMENTS  ->  BHoM NodeDisplacement ----
            if (tableName.Contains("JOINT") && tableName.Contains("DISPLACEMENT"))
                return (
                    Headers: new[] { "ux", "uy", "uz", "rx", "ry", "rz",
                                  "object_id", "resultcase", "modenumber", "timestep", "orientation" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a translation and a joint id, skip it...
                        if (!row.ContainsKey("u1") || !row.ContainsKey("joint")) return (string[])null;
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Translations: u1, u2, u3
                            GetValue(row, "u1"), GetValue(row, "u2"), GetValue(row, "u3"),
                            // Rotations: r1, r2, r3
                            GetValue(row, "r1"), GetValue(row, "r2"), GetValue(row, "r3"),
                            // Object ID and Result Case
                            GetValue(row, "joint"), GetValue(row, "outputcase"),
                            // ModeNumberow, Timestep and Orientation (constants)
                            "0", "0", null };
                    }
                );

            // ---- JOINT REACTIONS  ->  BHoM NodeReaction ----
            if (tableName.Contains("JOINT") && tableName.Contains("REACTION"))
                return (
                    Headers: new[] { "fx", "fy", "fz", "mx", "my", "mz",
                                  "object_id", "resultcase", "modenumber", "timestep", "orientation" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a force and a joint id, skip it...
                        if (!row.ContainsKey("f1") || !row.ContainsKey("joint")) return (string[])null;
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Forces: f1, f2, f3
                            GetValue(row, "f1"), GetValue(row, "f2"), GetValue(row, "f3"),
                            // Moments: m1, m2, m3
                            GetValue(row, "m1"), GetValue(row, "m2"), GetValue(row, "m3"),
                            // Object ID and Result Case
                            GetValue(row, "joint"), GetValue(row, "outputcase"),
                            // ModeNumberow, Timestep and Orientation (constants)
                            "0", "0",  null };
                    }
                );

            // ---- ELEMENT FORCES - AREA SHELLS  ->  BHoM MeshForce ----
            if (tableName.Contains("ELEMENT") && tableName.Contains("FORCES") &&
               (tableName.Contains("SHELL") || tableName.Contains("AREA")))
                return (
                    Headers: new[] { "object_id", "node_id", "resultcase", "modenumber", "timestep",
                                  "nxx", "nyy", "nxy", "mxx", "myy", "mxy", "vx", "vy" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a force and a joint id, skip it...
                        if (!row.ContainsKey("f11") || !row.ContainsKey("area")) return (string[])null;
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Object ID and Node ID (per-joint result)
                            GetValue(row, "area"), GetValue(row, "joint"),
                            // Result Case, ModeNumberow, Timestep (constants)
                            GetValue(row, "outputcase"), "0", "0",            
                            // Membrane Forces: f11, f22, f12
                            GetValue(row, "f11"), GetValue(row, "f22"), GetValue(row, "f12"),
                            // Bending Moments: m11, m22, m12
                            GetValue(row, "m11"), GetValue(row, "m22"), GetValue(row, "m12"),
                            // Transverse Shears: v13, v23
                            GetValue(row, "v13"), GetValue(row, "v23") };
                    }
                );

            // ---- BASE REACTIONS  ->  BHoM GlobalReactions (no element id; global) ----
            if (tableName.Contains("BASE") && tableName.Contains("REACTION"))
                return (
                    Headers: new[] { "fx", "fy", "fz", "mx", "my", "mz", "x", "y", "z",
                                  "resultcase", "modenumber", "timestep" },
                    RowMapper: row =>
                    {
                        // If the row doesn't contain at least a force, skip it...
                        if (!row.ContainsKey("globalfx")) return (string[])null;
                        // ...otherwise, return the mapped values in the order of the CSV headers.
                        return new[] {
                            // Reaction Forces: fx, fy, fz
                            GetValue(row, "globalfx"), GetValue(row, "globalfy"), GetValue(row, "globalfz"),
                            // Reaction Moments: mx, my, mz
                            GetValue(row, "globalmx"), GetValue(row, "globalmy"), GetValue(row, "globalmz"),
                            // Reaction Point: x, y, z
                            GetValue(row, "globalx"), GetValue(row, "globaly"), GetValue(row, "globalz"),
                            // Result Case, ModeNumberow, Timestep (constants)
                            GetValue(row, "outputcase"), "0", "0" };
                    }
                );

            // If the table name matches none of the above, log error, return error message in the console and return null outputs.
            BH.Engine.Base.Compute.RecordError("Unrecognized SAP2000 results table: \"" + tableName + "\".");
            Console.WriteLine("Unrecognized SAP2000 results table: \"" + tableName + "\".");
            return (null, null);
        }
    }
}


