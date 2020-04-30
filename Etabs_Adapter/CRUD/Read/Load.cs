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
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.Engine.ETABS;
using BH.oM.Geometry;
using BH.Engine.Structure;
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
        /****       Read Methods                        ****/
        /***************************************************/

        private List<ILoad> ReadLoad(Type type, List<string> ids = null)
        {
            List<ILoad> loadList = new List<ILoad>();
            
            List<Loadcase> loadcaseList = ReadLoadcase();

            if (ids != null)
            {
                Engine.Reflection.Compute.RecordWarning("Id filtering is not implemented for Loads, all Loads will be returned.");
            }

            if (type == typeof(PointLoad))
                return ReadPointLoad(loadcaseList);
            else if (type == typeof(BarUniformlyDistributedLoad))
                return ReadBarLoad(loadcaseList);
            else if (type == typeof(BarVaryingDistributedLoad))
                return ReadBarVaryingLoad(loadcaseList);
            else if (type == typeof(AreaUniformlyDistributedLoad))
                return ReadAreaLoad(loadcaseList);
            else if (type == typeof(AreaTemperatureLoad))
                return ReadAreaTempratureLoad(loadcaseList);
            else if (type == typeof(BarTemperatureLoad))
                return ReadBarTempratureLoad(loadcaseList);
            else if (type == typeof(BarPointLoad))
                return ReadBarPointLoad(loadcaseList);
            else
            {
                List<ILoad> loads = new List<ILoad>();
                loads.AddRange(ReadPointLoad(loadcaseList));
                loads.AddRange(ReadBarLoad(loadcaseList));
                loads.AddRange(ReadAreaLoad(loadcaseList));
                return loads;
            }
        }

        /***************************************************/

        private List<ILoad> ReadPointLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();

            Dictionary<string, Node> bhomNodes = ReadNode().ToDictionary(x => x.CustomData[AdapterIdName].ToString());

            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] step = null;
            int nameCount = 0;
            double[] fx = null;
            double[] fy = null;
            double[] fz = null;
            double[] mx = null;
            double[] my = null;
            double[] mz = null;

            if (m_model.PointObj.GetLoadForce("All", ref nameCount, ref names, ref loadcase, ref step, ref CSys, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, eItemType.Group) != 0)
                return bhLoads;

            for (int i = 0; i < nameCount; i++)
            {
                if (CSys[i] != "Global")
                    Engine.Reflection.Compute.RecordWarning($"The coordinate system: {CSys[i]} was not read. The PointLoads defined in the coordinate system: {CSys[i]} were set as Global");

                Loadcase bhLoadcase = loadcases.FirstOrDefault(x => x.Name == loadcase[i]);

                if (bhLoadcase == null)
                    continue;

                PointLoad bhLoad = new PointLoad()
                {
                    Force = new Vector() { X = fx[i], Y = fy[i], Z = fz[i] },
                    Moment = new Vector() { X = mx[i], Y = my[i], Z = mz[i] },
                    Loadcase = bhLoadcase,
                    Objects = new BHoMGroup<Node>() { Elements = { bhomNodes[names[i]] } }
                };
                bhLoads.Add(bhLoad);
            }

            return bhLoads;

        }

        /***************************************************/

        private List<ILoad> ReadBarLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();
            
            Dictionary<string, Bar> bhomBars = ReadBar().ToDictionary(x => x.CustomData[AdapterIdName].ToString());

            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] myType = null;
            int[] dir = null;
            int nameCount = 0;
            double[] rd1 = null;
            double[] rd2 = null;
            double[] dist1 = null;
            double[] dist2 = null;
            double[] val1 = null;
            double[] val2 = null;

            if (m_model.FrameObj.GetLoadDistributed("All", ref nameCount, ref names, ref loadcase, ref myType, ref CSys, ref dir, ref rd1, ref rd2, ref dist1, ref dist2, ref val1, ref val2, eItemType.Group) != 0)
                return bhLoads;

            for (int i = 0; i < nameCount; i++)
            {
                Loadcase bhLoadcase = loadcases.FirstOrDefault(x => x.Name == loadcase[i]);

                if (bhLoadcase == null)
                    continue;
                if (dist1[i] != 0 || rd2[i] != 1 || Math.Abs(val1[i] - val2[i]) > Tolerance.Distance)   //Is Varying
                    continue;

                Vector force = Direction(dir[i], val1[i]);
                
                BHoMGroup<Bar> barObjects = new BHoMGroup<Bar>() { Elements = { bhomBars[names[i]] } };

                BarUniformlyDistributedLoad bhLoad = new BarUniformlyDistributedLoad()
                {
                    Loadcase = bhLoadcase,
                    Objects = barObjects
                };

                switch (myType[i])
                {
                    case 1:
                        bhLoad.Force = force;
                        break;
                    case 2:
                        bhLoad.Moment = force;
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("Could not create the load. It's not 'MyType'. MyType = " + myType[i].ToString());
                        break;
                }

                SetDirection(bhLoad, dir[i], CSys[i]);

                bhLoads.Add(bhLoad);
            }
            return bhLoads;

        }

        /***************************************************/

        private List<ILoad> ReadBarVaryingLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();

            Dictionary<string, Bar> bhomBars = ReadBar().ToDictionary(x => x.CustomData[AdapterIdName].ToString());

            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] myType = null;
            int[] dir = null;
            int nameCount = 0;
            double[] rd1 = null;
            double[] rd2 = null;
            double[] dist1 = null;
            double[] dist2 = null;
            double[] val1 = null;
            double[] val2 = null;

            if (m_model.FrameObj.GetLoadDistributed("All", ref nameCount, ref names, ref loadcase, ref myType, ref CSys, ref dir, ref rd1, ref rd2, ref dist1, ref dist2, ref val1, ref val2, eItemType.Group) != 0)
                return bhLoads;

            for (int i = 0; i < nameCount; i++)
            {
                Loadcase bhLoadcase = loadcases.FirstOrDefault(x => x.Name == loadcase[i]);

                if (bhLoadcase == null)
                    continue;

                if (dist1[i] == 0 && rd2[i] == 1 && Math.Abs(val1[i] - val2[i]) < Tolerance.Distance)   //Is uniform
                    continue;

                Vector forceA = Direction(dir[i], val1[i]);
                Vector forceB = Direction(dir[i], val2[i]);

                Bar bhBar = bhomBars[names[i]];
                BHoMGroup<Bar> barObjects = new BHoMGroup<Bar>() { Elements = { bhBar } };

                BarVaryingDistributedLoad bhLoad = new BarVaryingDistributedLoad()
                {
                    DistanceFromA = dist1[i],
                    DistanceFromB = bhBar.Length() - dist2[i],
                    Loadcase = bhLoadcase,
                    Objects = barObjects
                };

                switch (myType[i])
                {
                    case 1:
                        bhLoad.ForceA = forceA;
                        bhLoad.ForceB = forceB;
                        break;
                    case 2:
                        bhLoad.MomentA = forceA;
                        bhLoad.MomentB = forceB;
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("Could not create the load. It's not 'MyType'. MyType = " + myType[i].ToString());
                        break;
                }

                SetDirection(bhLoad, dir[i], CSys[i]);

                bhLoads.Add(bhLoad);
            }

            return bhLoads;
        }

        /***************************************************/

        private List<ILoad> ReadAreaLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();

            Dictionary<string, Panel> bhomPanels = ReadPanel().ToDictionary(x => x.CustomData[AdapterIdName].ToString());

            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] dir = null;
            int nameCount = 0;
            double[] f = null;

            if (m_model.AreaObj.GetLoadUniform("All", ref nameCount, ref names, ref loadcase, ref CSys, ref dir, ref f, eItemType.Group) != 0)
                return bhLoads;

            for (int i = 0; i < nameCount; i++)
            {
                Loadcase bhLoadcase = loadcases.FirstOrDefault(x => x.Name == loadcase[i]);

                if (bhLoadcase == null)
                    continue;

                BHoMGroup<IAreaElement> panelObjects = new BHoMGroup<IAreaElement>() { Elements = { bhomPanels[names[i]] } };

                Vector pressure = Direction(dir[i], f[i]);
                if (CSys[i] == "Local")
                {
                    double temp = -pressure.Y;
                    pressure.Y = pressure.Z;
                    pressure.Z = temp;
                }

                AreaUniformlyDistributedLoad bhAreaUniLoad = new AreaUniformlyDistributedLoad()
                {
                    Pressure = pressure,
                    Loadcase = bhLoadcase,
                    Objects = panelObjects
                };

                SetDirection(bhAreaUniLoad, dir[i], CSys[i]);

                bhLoads.Add(bhAreaUniLoad);
            }

            return bhLoads;
        }

        /***************************************************/

        private List<ILoad> ReadAreaTempratureLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();

            Dictionary<string, Panel> bhomPanels = ReadPanel().ToDictionary(x => x.CustomData[AdapterIdName].ToString());

            string[] names = null;
            string[] loadcase = null;
            int[] myType = null;
            double[] value = null;
            int nameCount = 0;
            string[] patternName = null;

            if (m_model.AreaObj.GetLoadTemperature("All", ref nameCount, ref names, ref loadcase, ref myType, ref value, ref patternName, eItemType.Group) != 0)
                return bhLoads;

            for (int i = 0; i < nameCount; i++)
            {
                Loadcase bhLoadcase = loadcases.FirstOrDefault(x => x.Name == loadcase[i]);

                if (bhLoadcase == null)
                    continue;

                if (myType[i] == 1)
                {
                    BHoMGroup<IAreaElement> panelObjects = new BHoMGroup<IAreaElement>() { Elements = { bhomPanels[names[i]] } };

                    bhLoads.Add(new AreaTemperatureLoad() { Loadcase = bhLoadcase, Objects = panelObjects, TemperatureChange = value[i] });
                }
            }

            return bhLoads;
        }

        /***************************************************/

        private List<ILoad> ReadBarTempratureLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();

            Dictionary<string, Bar> bhomBars = ReadBar().ToDictionary(x => x.CustomData[AdapterIdName].ToString());

            string[] names = null;
            string[] loadcase = null;
            int[] myType = null;
            double[] value = null;
            string[] patternName = null;
            int nameCount = 0;

            if (m_model.FrameObj.GetLoadTemperature("All", ref nameCount, ref names, ref loadcase, ref myType, ref value, ref patternName, eItemType.Group) != 0)
                return bhLoads;

            for (int i = 0; i < nameCount; i++)
            {
                Loadcase bhLoadcase = loadcases.FirstOrDefault(x => x.Name == loadcase[i]);

                if (bhLoadcase == null)
                    continue;

                if (myType[i] == 1)
                {
                    BHoMGroup<Bar> barObjects = new BHoMGroup<Bar>() { Elements = { bhomBars[names[i]] } };

                    bhLoads.Add(new BarTemperatureLoad() { TemperatureChange = value[i], Loadcase = bhLoadcase, Objects = barObjects });
                }
            }

            return bhLoads;
        }

        /***************************************************/

        private List<ILoad> ReadBarPointLoad(List<Loadcase> loadcases)
        {
            List<ILoad> bhLoads = new List<ILoad>();

            Dictionary<string, Bar> bhomBars = ReadBar().ToDictionary(x => x.CustomData[AdapterIdName].ToString());

            string[] names = null;
            string[] loadcase = null;
            string[] CSys = null;
            int[] myType = null;
            int[] dir = null;
            int nameCount = 0;
            double[] relDist = null;
            double[] dist = null;
            double[] value = null;

            if (m_model.FrameObj.GetLoadPoint("All", ref nameCount, ref names, ref loadcase, ref myType, ref CSys, ref dir, ref relDist, ref dist, ref value, eItemType.Group) != 0)
                return bhLoads;

            for (int i = 0; i < nameCount; i++)
            {
                Loadcase bhLoadcase = loadcases.FirstOrDefault(x => x.Name == loadcase[i]);

                if (bhLoadcase == null)
                    continue;

                Vector force = Direction(dir[i], value[i]);

                BHoMGroup < Bar> barObjects = new BHoMGroup<Bar>() { Elements = { bhomBars[names[i]] } };

                BarPointLoad bhBarPointLoad = new BarPointLoad()
                {
                    DistanceFromA = dist[i],
                    Loadcase = bhLoadcase,
                    Objects = barObjects
                };

                switch (myType[i])
                {
                    case 1:
                        bhBarPointLoad.Force = force;
                        break;
                    case 2:
                        bhBarPointLoad.Moment = force;
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("Could not create the load. It's not 'MyType'. MyType = " + myType[i].ToString());
                        continue;
                }

                SetDirection(bhBarPointLoad, dir[i], CSys[i]);

                bhLoads.Add(bhBarPointLoad);
            }
            
            return bhLoads;
        }

        /***************************************************/
        /****       Helper Methods                      ****/
        /***************************************************/

        private void SetDirection(ILoad load, int dir, string cSys)
        {
            if (cSys != "Global" && cSys != "Local")
                Engine.Reflection.Compute.RecordWarning($"Custom coordinatesystem {cSys} for loads have been set as Global");
            
            int type = (int)Math.Floor((double)((dir - 1) / 3));
            switch (type)
            {
                case 0:
                    load.Axis = LoadAxis.Local;
                    load.Projected = false;
                    break;
                case 1:
                    load.Axis = LoadAxis.Global;
                    load.Projected = false;
                    break;
                case 2:
                    load.Axis = LoadAxis.Global;
                    load.Projected = true;
                    break;
                case 3:     // Gravity
                    load.Axis = LoadAxis.Global;
                    load.Projected = dir == 11;
                    break;
                default:
                    break;
            }
        }

        /***************************************************/

        private Vector Direction(int dir, double val)
        {
            Vector vector = new Vector();

            switch (dir)
            {
                case 1:
                    vector.X = val;
                    break;
                case 2:
                    vector.Z = val;
                    break;
                case 3:
                    vector.Y = -val;
                    break;
                case 4:
                    vector.X = val;
                    break;
                case 5:
                    vector.Y = val;
                    break;
                case 6:
                    vector.Z = val;
                    break;
                case 7:
                    vector.X = val;
                    break;
                case 8:
                    vector.Y = val;
                    break;
                case 9:
                    vector.Z = -val;
                    break;
                case 10:
                    vector.Z = -val;
                    break;
                case 11:
                    vector.Z = -val;
                    break;
                default:
                    break;
            }
            return vector;
        }

        /***************************************************/

    }
}

