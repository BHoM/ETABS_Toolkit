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
#else
using ETABS2016;
#endif

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        
        private List<ILoad> ReadLoad(Type type, List<string> ids = null)
        {
            List<ILoad> loadList = new List<ILoad>();
            
            List<Loadcase> loadcaseList = ReadLoadcase();

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

                Vector force = new Vector();

                switch (dir[i])
                {
                    case 4:
                        force.X = val1[i];
                        break;
                    case 5:
                        force.Y = val1[i];
                        break;
                    case 6:
                        force.Z = val1[i];
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("That load direction is not supported. Dir = " + dir[i].ToString());
                        break;
                }
                BHoMGroup<Bar> barObjects = new BHoMGroup<Bar>() { Elements = { bhomBars[names[i]] } };

                switch (myType[i])
                {
                    case 1:
                        bhLoads.Add(new BarUniformlyDistributedLoad() { Force = force, Loadcase = bhLoadcase, Objects = barObjects });
                        break;
                    case 2:
                        bhLoads.Add(new BarUniformlyDistributedLoad() { Moment = force, Loadcase = bhLoadcase, Objects = barObjects });
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("Could not create the load. It's not 'MyType'. MyType = " + myType[i].ToString());
                        break;
                }

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

                if (bhLoadcase.Name != loadcase[i])
                    continue;
                if (dist1[i] == 0 && rd2[i] == 1 && Math.Abs(val1[i] - val2[i]) < Tolerance.Distance)   //Is uniform
                    continue;

                Vector forceA = new Vector();
                Vector forceB = new Vector();

                switch (dir[i])
                {
                    case 4:
                        forceA.X = val1[i];
                        forceB.X = val2[i];
                        break;
                    case 5:
                        forceA.Y = val1[i];
                        forceB.Y = val2[i];
                        break;
                    case 6:
                        forceA.Z = val1[i];
                        forceB.Z = val2[i];
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("That load direction is not supported. Dir = " + dir[i].ToString());
                        break;
                }
                Bar bhBar = bhomBars[names[i]];
                BHoMGroup<Bar> barObjects = new BHoMGroup<Bar>() { Elements = { bhBar } };

                switch (myType[i])
                {
                    case 1:
                        bhLoads.Add(new BarVaryingDistributedLoad()
                        {
                            ForceA = forceA,
                            ForceB = forceB,
                            DistanceFromA = dist1[i],
                            DistanceFromB = bhBar.Length() - dist2[i],
                            Loadcase = bhLoadcase,
                            Objects = barObjects
                        });
                        break;
                    case 2:
                        bhLoads.Add(new BarVaryingDistributedLoad()
                        {
                            MomentA = forceA,
                            MomentB = forceB,
                            DistanceFromA = dist1[i],
                            DistanceFromB = bhBar.Length() - dist2[i],
                            Loadcase = bhLoadcase,
                            Objects = barObjects
                        });
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("Could not create the load. It's not 'MyType'. MyType = " + myType[i].ToString());
                        break;
                }
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

            Vector pressure = new Vector();
            for (int i = 0; i < nameCount; i++)
            {
                Loadcase bhLoadcase = loadcases.FirstOrDefault(x => x.Name == loadcase[i]);

                if (bhLoadcase == null)
                    continue;

                BHoMGroup<IAreaElement> panelObjects = new BHoMGroup<IAreaElement>() { Elements = { bhomPanels[names[i]] } };

                switch (dir[i])
                {
                    case 4:
                        pressure.X = f[i];
                        break;
                    case 5:
                        pressure.Y = f[i];
                        break;
                    case 6:
                        pressure.Z = f[i];
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("That load direction is not supported. Dir = " + dir[i].ToString());
                        break;
                }

                bhLoads.Add(new AreaUniformlyDistributedLoad() { Loadcase = bhLoadcase, Pressure = pressure, Objects = panelObjects });
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

                Vector force = new Vector();
                switch (dir[i])
                {
                    case 4:
                        force.X = value[i];
                        break;
                    case 5:
                        force.Y = value[i];
                        break;
                    case 6:
                        force.Z = value[i];
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("That load direction is not supported. Dir = " + dir[i].ToString());
                        break;
                }
                BHoMGroup<Bar> barObjects = new BHoMGroup<Bar>() { Elements = { bhomBars[names[i]] } };

                switch (myType[i])
                {
                    case 1:
                        bhLoads.Add(new BarPointLoad() { Force = force, DistanceFromA = dist[i], Loadcase = bhLoadcase, Objects = barObjects });
                        break;
                    case 2:
                        bhLoads.Add(new BarPointLoad() { Moment = force, DistanceFromA = dist[i], Loadcase = bhLoadcase, Objects = barObjects });
                        break;
                    default:
                        BH.Engine.Reflection.Compute.RecordWarning("Could not create the load. It's not 'MyType'. MyType = " + myType[i].ToString());
                        break;
                }
            }
            
            return bhLoads;
        }

        /***************************************************/

    }
}

