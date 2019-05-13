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
using BH.Adapter.ETABS;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.SectionProperties;
using BH.oM.Geometry.ShapeProfiles;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Elements;
using BH.oM.Geometry;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.DataManipulation.Queries;
using ETABS2016;

namespace ETABS_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //ETABSAdapter app = new ETABSAdapter(null, null, true);

            TestBarReleases();

            //MeshResults(app);
            //TestPushElements(app);
            //TestPullBars(app);
            //ExampleLevels3();
            //ExampleStories();
        }

        private static void TestBarReleases()
        {
            cSapModel m_model;

            cOAPI m_app;
            string pathToETABS = @"C:\Program Files\Computers and Structures\ETABS 2016\ETABS.exe";
            cHelper helper = new ETABS2016.Helper();


            //open ETABS if not running - NOTE: this behaviour is different from other adapters
            m_app = helper.CreateObject(pathToETABS);
            m_app.ApplicationStart();

            m_model = m_app.SapModel;

            m_model.InitializeNewModel();
            m_model.File.NewBlank();

            string name = "1";

            m_model.FrameObj.AddByCoord(0, 0, 0, 0, 0, 10, ref name);

            bool[] ii = new bool[6];
            bool[] jj = new bool[6];
            double[] startVal = new double[6];
            double[] endVal = new double[6];
            ii[5] = true;
            jj[5] = true;

            int ret = m_model.FrameObj.SetReleases(name, ref ii, ref jj, ref startVal, ref endVal);


        }


        private static void MeshResults(ETABSAdapter app)
        {
            var results = app.Pull(new FilterQuery { Type = typeof(BH.oM.Structure.Results.MeshForce) });
        }

        private static void ExampleStories()
        {
            cSapModel m_model;
            
            cOAPI m_app;
            int ret = -1;
            int NumberStories = 0;
            string[] StoryNames = { };
            double[] StoryHeights = { };
            double[] StoryElevations = { };
            bool[] IsMasterStory = { };
            string[] SimilarToStory = { };
            bool[] SpliceAbove = { };
            double[] SpliceHeight = { };
            // create ETABS object
            var runningInstance = System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");

            m_app = (cOAPI)runningInstance;
            m_model = m_app.SapModel;
            // set stories

            string[] inStoryNames =
                {
   "BaseStory",
                "Foundations",
                "Basement 02",
                "Basement 01",
                "Level 01",
                "Level 02",
                "Level 03",
                "Level 04",
                "Level 05",
                "Level 06",
                "Roof Level",
                "Roof Edge"
            };
            double[] inStoryElevations = 
            {
                    0,
                    5.98,
                    6.78,
                    10.94,
                    15.42,
                    19.9,
                    24.38,
                    28.7,
                    32.06,
                    36.06,
                    40.14,
                    41.0
                }; 
            double[] inStoryHeights = {
            2,
0.8,
4.15,
4.45,
4.5,
4.5,
4.3,
3.35,
4,
4.1,
0.85,
1
};


            //string[] inStoryNames =
            //    {
            //"Foundations",
            //    "Basement 02",
            //    "Basement 01",
            //    "Level 01",
            //    "Level 02",
            //    "Level 03",
            //};
            //double[] inStoryElevations = {
            //0,
            //5,
            //15,
            //23,
            //23.5,
            //40,
            //50 };
            //double[] inStoryHeights = {
            //2,
            //5,
            //7,
            //33,
            //1,
            //9};
        bool[] inIsMasterStory = {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
                        false,
            true };
            //    string[] inSimilarToStory =
            //        {
            //    "None",
            //    "",
            //    "Level 03",
            //    "Level 03",
            //    "Level 03",
            //    "Level 03"
            //};

            string[] inSimilarToStory =
    {
            "None",
            "None",
            "None",
            "None",
            "None",
            "None",
            "None",
            "None",
            "None",
            "None",
            "None",
            "None"
        };
            //bool[] inSpliceAbove = {
            //false,
            //true,
            //false,
            //true,
            //false,
            //true };
            //double[] inSpliceHeight = {
            //0,
            //0,
            //2,
            //2,
            //0,
            //1 };

            bool[] inSpliceAbove = {
            false,
            false,
            false,
            false,
            false,
            false,
               false,
            false,
            false,
            false,
            false,
            false};
            double[] inSpliceHeight = {
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0};

            ret = m_model.Story.SetStories(inStoryNames, inStoryElevations, inStoryHeights, inIsMasterStory, inSimilarToStory, inSpliceAbove, inSpliceHeight);
            // get stories
            ret = m_model.Story.GetStories(ref NumberStories, ref StoryNames, ref StoryHeights, ref StoryElevations, ref IsMasterStory, ref SimilarToStory, ref SpliceAbove, ref SpliceHeight);
        }

        private static void ExampleLevels3()
        {
            double[] elevations = new double[]
                {
                    5.98,
                    6.78,
                    10.94,
                    15.42,
                    19.9,
                    24.38,
                    28.7,
                    32.06,
                    36.06,
                    40.14,
                    41.0
                };

            string[] names = new string[]
            {
                "Foundations",
                "Basement 02",
                "Basement 01",
                "Level 01",
                "Level 02",
                "Level 03",
                "Level 04",
                "Level 05",
                "Level 06",
                "Roof Level",
                "Roof Edge"
            };

            List<BH.oM.Architecture.Elements.Level> levels = new List<BH.oM.Architecture.Elements.Level>();

            for (int i = 0; i < elevations.Length; i++)
            {
                levels.Add(new BH.oM.Architecture.Elements.Level { Elevation = elevations[i], Name = names[i] });
            }
           

            ETABSAdapter adapter = new ETABSAdapter("", null,true);

            adapter.Push(levels);
        }


        private static void ExampleLevels2()
        {
            cSapModel m_model;
            cOAPI m_app;

            // create ETABS object
            var runningInstance = System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.ETABS.API.ETABSObject");

            m_app = (cOAPI)runningInstance;
            m_model = m_app.SapModel;

            double[] elevations = new double[]
                {
                    5.98,
                    6.78,
                    10.94,
                    15.42,
                    19.9,
                    24.38,
                    28.7,
                    32.06,
                    36.06,
                    40.14,
                    41.0
                };

            string[] names = new string[]
            {
                "Foundations",
                "Basement 02",
                "Basement 01",
                "Level 01",
                "Level 02",
                "Level 03",
                "Level 04",
                "Level 05",
                "Level 06",
                "Roof Level",
                "Roof Edge"
            };

            List<BH.oM.Architecture.Elements.Level> levels = new List<BH.oM.Architecture.Elements.Level>();

            for (int i = 0; i < elevations.Length; i++)
            {
                levels.Add(new BH.oM.Architecture.Elements.Level { Elevation = elevations[i], Name = names[i] });
            }

            double[] heights = new double[levels.Count];

            elevations = levels.Select(x => x.Elevation).ToArray();

            //elevations = elevations.Select(x => Math.Round(x * 20)).ToArray();

            for (int i = 0; i < levels.Count-1; i++)
            {
                heights[i] = levels[i + 1].Elevation - levels[i].Elevation;
            }

            heights = new double[levels.Count];

            elevations = new double[levels.Count + 1];

            for (int i = 0; i < levels.Count; i++)
            {
                elevations[i + 1] = levels[i].Elevation;
            }

            heights[heights.Length - 1] = 1;

            //elevations = elevations.Select(x => x / 20).ToArray();
            //heights = heights.Select(x => x / 20).ToArray();



            names = levels.Select(x => x.Name).ToArray();

            bool[] isMaster = new bool[names.Length];

            isMaster[isMaster.Length - 1] = true;

            string[] similarTo = new string[names.Length];

            for (int i = 0; i < similarTo.Length-1; i++)
            {
                similarTo[i] = "None";//names[names.Length - 1];
            }
            similarTo[similarTo.Length - 1] = "None";

            bool[] spliceAbove = new bool[names.Length];
            double[] spliceHeight = new double[names.Length];

            int ret = m_model.Story.SetStories(names, elevations, heights, isMaster, similarTo, spliceAbove, spliceHeight);


        }

        //private static void TestPushElements(ETABSAdapter app)
        //{
        //    Console.WriteLine("Testing Push Bars ...");

        //    Point p1 = new Point { X = 0, Y = 0, Z = 0 };
        //    Point p2 = new Point { X = 1, Y = 0, Z = 0 };
        //    Point p3 = new Point { X = 1, Y = 1, Z = 0 };
        //    Point p4 = new Point { X = 0, Y = 1, Z = 0 };
        //    Point p5 = new Point { X = 0, Y = 0, Z = 1 };
        //    Point p6 = new Point { X = 1, Y = 0, Z = 1 };
        //    Point p7 = new Point { X = 1, Y = 1, Z = 1 };
        //    Point p8 = new Point { X = 0, Y = 1, Z = 1 };

        //    Point p5b = new Point { X = 0, Y = 0, Z = 2 };
        //    Point p6b = new Point { X = 1, Y = 0, Z = 2 };
        //    Point p7b = new Point { X = 1, Y = 1, Z = 2 };
        //    Point p8b = new Point { X = 0, Y = 1, Z = 2 };

        //    Constraint6DOF pin = BH.Engine.Structure.Create.PinConstraint6DOF();
        //    Constraint6DOF fix = BH.Engine.Structure.Create.FixConstraint6DOF();
        //    Constraint6DOF full = BH.Engine.Structure.Create.FullReleaseConstraint6DOF();

        //    List<Node> nodesA = new List<Node>();

        //    Node n1a = BH.Engine.Structure.Create.Node(p5 , "1");
        //    Node n2a = BH.Engine.Structure.Create.Node( p6 , "2" );
        //    Node n3a = BH.Engine.Structure.Create.Node( p7 , "3" );
        //    Node n4a = BH.Engine.Structure.Create.Node( p8 , "4" );

        //    n1a.Constraint = pin;
        //    n2a.Constraint = pin;
        //    n3a.Constraint = fix;
        //    n4a.Constraint = fix;

        //    nodesA.Add(n1a);
        //    nodesA.Add(n2a);
        //    nodesA.Add(n3a);
        //    nodesA.Add(n4a);



        //    List<Node> nodesB = new List<Node>();

        //    Node n1b = BH.Engine.Structure.Create.Node( p5b, "1" );
        //    Node n2b = BH.Engine.Structure.Create.Node( p6b,  "2" );
        //    Node n3b = BH.Engine.Structure.Create.Node( p7b,  "3" );
        //    Node n4b = BH.Engine.Structure.Create.Node( p8b,  "4" );

        //    n1b.Constraint = pin;
        //    n2b.Constraint = pin;
        //    n3b.Constraint = full;
        //    n4b.Constraint = fix;

        //    nodesB.Add(n1b);
        //    nodesB.Add(n2b);
        //    nodesB.Add(n3b);
        //    nodesB.Add(n4b);

        //    Bar bar1 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p1), BH.Engine.Structure.Create.Node( p2));
        //    Bar bar2 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p2), BH.Engine.Structure.Create.Node( p3));
        //    Bar bar3 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p3), BH.Engine.Structure.Create.Node( p4));
        //    Bar bar4 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p4), BH.Engine.Structure.Create.Node( p1));

        //    Bar bar5 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p5), BH.Engine.Structure.Create.Node( p6));
        //    Bar bar6 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p6), BH.Engine.Structure.Create.Node( p7));
        //    Bar bar7 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p7), BH.Engine.Structure.Create.Node( p8));
        //    Bar bar8 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p8), BH.Engine.Structure.Create.Node( p5));

        //    Bar bar9 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p1), BH.Engine.Structure.Create.Node( p5));
        //    Bar bar10 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p2), BH.Engine.Structure.Create.Node( p6));
        //    Bar bar11 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p3), BH.Engine.Structure.Create.Node( p7));
        //    Bar bar12 = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p4), BH.Engine.Structure.Create.Node( p8));

        //    Bar bar5b = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p5b), BH.Engine.Structure.Create.Node( p6b));
        //    Bar bar6b = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p6b), BH.Engine.Structure.Create.Node( p7b));
        //    Bar bar7b = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p7b), BH.Engine.Structure.Create.Node( p8b));
        //    Bar bar8b = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p8b), BH.Engine.Structure.Create.Node( p5b));

        //    Bar bar9b = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p1), BH.Engine.Structure.Create.Node( p5b));
        //    Bar bar10b = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p2), BH.Engine.Structure.Create.Node( p6b));
        //    Bar bar11b = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p3), BH.Engine.Structure.Create.Node( p7b));
        //    Bar bar12b = BH.Engine.Structure.Create.Bar(BH.Engine.Structure.Create.Node( p4), BH.Engine.Structure.Create.Node( p8b));

        //    List<Bar> bars1 = new List<Bar>();
        //    List<Bar> bars2a = new List<Bar>();
        //    List<Bar> bars2b = new List<Bar>();

        //    bars1.Add(bar1);
        //    bars1.Add(bar2);
        //    bars1.Add(bar3);
        //    bars1.Add(bar4);

        //    bars2a.Add(bar5);
        //    bars2a.Add(bar6);
        //    bars2a.Add(bar7);
        //    bars2a.Add(bar8);
        //    bars2a.Add(bar9);
        //    bars2a.Add(bar10);
        //    bars2a.Add(bar11);
        //    bars2a.Add(bar12);

        //    bars2b.Add(bar5b);
        //    bars2b.Add(bar6b);
        //    bars2b.Add(bar7b);
        //    bars2b.Add(bar8b);
        //    bars2b.Add(bar9b);
        //    bars2b.Add(bar10b);
        //    bars2b.Add(bar11b);
        //    bars2b.Add(bar12b);

        //    //Material steel = BH.Engine.Common.Create.Material("Steel", MaterialType.Steel, 210000, 0.3, 0.00012, 78500);

        //    ISectionProperty sec1 = BH.Engine.Structure.Create.SteelISection(110, 10, 80, 20);
        //    //sec1.Material = steel;// BH.Engine.Common.Create.Material("Steel", MaterialType.Steel, 210000, 0.3, 0.00012, 78500); //BH.Engine.Common.Create.Material("blue steel");//<-- this creates material of type aluminium
        //    sec1.Name = "Section 1";

        //    ISectionProperty sec2a = BH.Engine.Structure.Create.ConcreteRectangleSection(200, 120);
        //    //sec2a.Material = BH.Engine.Common.Create.Material("myConcrete", MaterialType.Concrete, 10, 10, 10, 10);
        //    sec2a.Name = "Section 2a";

        //    ISectionProperty sec2b = new ExplicitSection();
        //    //sec2b.Material = BH.Engine.Common.Create.Material("otherSteel", MaterialType.Steel, 210000, 0.3, 0.00012, 78500);
        //    sec2b.Name = "Section 2b";


        //    foreach (Bar b in bars1)
        //        b.SectionProperty = sec1;

        //    foreach (Bar b in bars2a)
        //        b.SectionProperty = sec2a;

        //    foreach (Bar b in bars2b)
        //        b.SectionProperty = sec2b;


        //    List<Panel> panels = new List<Panel>();
        //    Polyline outline = new Polyline();
        //    outline.ControlPoints = new List<Point>() { p1, p2, p3, p4, p1 };
        //    // Material steel = sec1.Material;// BH.Engine.Common.Create.Material("panelSteel");
        //    ISurfaceProperty panelProp = BH.Engine.Structure.Create.ConstantThickness(100, steel);
        //    panelProp.Name = "panelProperty";
        //    List<ICurve> nothing = null;
        //    Panel panelA = BH.Engine.Structure.Create.Panel(outline, nothing);
        //    panelA.Property = panelProp;
        //    panels.Add(panelA);

        //    outline.ControlPoints = new List<Point>() { p5, p6, p7, p8, p5 };
        //    Point op5 = new Point { X = 0.2, Y = 0.2, Z = 1 };
        //    Point op6 = new Point { X = 0.8, Y = 0.2, Z = 1 };
        //    Point op7 = new Point { X = 0.8, Y = 0.8, Z = 1 };
        //    Point op8 = new Point { X = 0.2, Y = 0.8, Z = 1 };
        //    Polyline hole = new Polyline() { ControlPoints = new List<Point>() { op5, op6, op7, op8 } };
        //    Opening opening = new Opening() { Edges = new List<Edge>() { new Edge() { Curve = hole } } };
        //    Panel panelB = BH.Engine.Structure.Create.Panel(outline, new List<Opening>() { opening });
        //    panelB.Property = panelProp;
        //    panels.Add(panelB);


        //    app.Push(nodesA, "Nodes");
        //    app.Push(nodesB, "Nodes");
        //    app.Push(bars1, "Bars1");
        //    app.Push(bars2a, "Bars2");
        //    app.Push(bars2b, "Bars2");

        //    app.Push(panels, "panels");

        //    Console.WriteLine("All elements Pushed !");
        //    Console.ReadLine();
        //}

        private static void TestPullBars(ETABSAdapter app)
        {
            Console.WriteLine("Test Pull Bars");
            FilterQuery nodeQuery = new FilterQuery { Type = typeof(Node) };
            FilterQuery barQuery = new FilterQuery { Type = typeof(Bar) };

            IEnumerable<object> barObjects = app.Pull(barQuery);

            int count = 0;
            foreach (object bObject in barObjects)
            {
                Bar bar = bObject as Bar;
                string barId = bar.CustomData[ETABSAdapter.ID].ToString();
                string startNodeId = bar.StartNode.CustomData[ETABSAdapter.ID].ToString();
                string endNodeId = bar.EndNode.CustomData[ETABSAdapter.ID].ToString();
                string startPoint = bar.StartNode.Position().X.ToString() + "," + bar.StartNode.Position().Y.ToString() + "," + bar.StartNode.Position().Z.ToString();
                string endPoint = bar.EndNode.Position().X.ToString() + "," + bar.EndNode.Position().Y.ToString() + "," + bar.EndNode.Position().Z.ToString();
                string section = bar.SectionProperty.Name;
                string material = bar.SectionProperty.Material.Name;

                string barInfo = "Bar with ID: " + barId + " -Connecting Nodes " + startNodeId + " at " + startPoint + " and " + endNodeId + " at " + endPoint + " Section: " + section + " Material: " + material + "/n";
                string barTags = string.Join("_/_", bar.Tags.ToArray());
                Console.WriteLine(barInfo + barTags);
            }

            Console.WriteLine("Pulled all bars");
            Console.WriteLine("... press enter to exit");
            Console.ReadLine();

        }
    }
}
