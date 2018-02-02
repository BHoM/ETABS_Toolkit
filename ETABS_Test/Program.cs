using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.Adapter.ETABS;
using BH.oM.Common.Materials;
using BH.oM.Structural.Properties;
using BH.oM.Structural.Elements;
using BH.oM.Geometry;
using BH.Engine.Structure;


namespace ETABS_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ETABSAdapter app = new ETABSAdapter();

            TestPushBars(app);
        }

        private static void TestPushBars(ETABSAdapter app)
        {
            Console.WriteLine("Testing Push Bars ...");

            Point p1 = new Point { X = 0, Y = 0, Z = 0 };
            Point p2 = new Point { X = 1, Y = 0, Z = 0 };
            Point p3 = new Point { X = 1, Y = 1, Z = 0 };
            Point p4 = new Point { X = 0, Y = 1, Z = 0 };
            Point p5 = new Point { X = 0, Y = 0, Z = 1 };
            Point p6 = new Point { X = 1, Y = 0, Z = 1 };
            Point p7 = new Point { X = 1, Y = 1, Z = 1 };
            Point p8 = new Point { X = 0, Y = 1, Z = 1 };

            Point p5b = new Point { X = 0, Y = 0, Z = 2 };
            Point p6b = new Point { X = 1, Y = 0, Z = 2 };
            Point p7b = new Point { X = 1, Y = 1, Z = 2 };
            Point p8b = new Point { X = 0, Y = 1, Z = 2 };

            Constraint6DOF pin = Create.PinConstraint6DOF();
            Constraint6DOF fix = Create.FixConstraint6DOF();
            Constraint6DOF full = Create.FullReleaseConstraint6DOF();

            List<Node> nodesA = new List<Node>();

            Node n1a = new Node { Position = p5, Name = "1" };
            Node n2a = new Node { Position = p6, Name = "2" };
            Node n3a = new Node { Position = p7, Name = "3" };
            Node n4a = new Node { Position = p8, Name = "4" };

            n1a.Constraint = pin;
            n2a.Constraint = pin;
            n3a.Constraint = fix;
            n4a.Constraint = fix;

            nodesA.Add(n1a);
            nodesA.Add(n2a);
            nodesA.Add(n3a);
            nodesA.Add(n4a);



            List<Node> nodesB = new List<Node>();

            Node n1b = new Node { Position = p5b, Name = "1" };
            Node n2b = new Node { Position = p6b, Name = "2" };
            Node n3b = new Node { Position = p7b, Name = "3" };
            Node n4b = new Node { Position = p8b, Name = "4" };

            n1b.Constraint = pin;
            n2b.Constraint = pin;
            n3b.Constraint = full;
            n4b.Constraint = fix;

            nodesB.Add(n1b);
            nodesB.Add(n2b);
            nodesB.Add(n3b);
            nodesB.Add(n4b);

            Bar bar1 = Create.Bar(new Node { Position = p1 }, new Node { Position = p2 });
            Bar bar2 = Create.Bar(new Node { Position = p2 }, new Node { Position = p3 });
            Bar bar3 = Create.Bar(new Node { Position = p3 }, new Node { Position = p4 });
            Bar bar4 = Create.Bar(new Node { Position = p4 }, new Node { Position = p1 });

            Bar bar5 = Create.Bar(new Node { Position = p5 }, new Node { Position = p6 });
            Bar bar6 = Create.Bar(new Node { Position = p6 }, new Node { Position = p7 });
            Bar bar7 = Create.Bar(new Node { Position = p7 }, new Node { Position = p8 });
            Bar bar8 = Create.Bar(new Node { Position = p8 }, new Node { Position = p5 });

            Bar bar9 = Create.Bar(new Node { Position = p1 }, new Node { Position = p5 });
            Bar bar10 = Create.Bar(new Node { Position = p2 }, new Node { Position = p6 });
            Bar bar11 = Create.Bar(new Node { Position = p3 }, new Node { Position = p7 });
            Bar bar12 = Create.Bar(new Node { Position = p4 }, new Node { Position = p8 });

            Bar bar5b = Create.Bar(new Node { Position = p5b }, new Node { Position = p6b });
            Bar bar6b = Create.Bar(new Node { Position = p6b }, new Node { Position = p7b });
            Bar bar7b = Create.Bar(new Node { Position = p7b }, new Node { Position = p8b });
            Bar bar8b = Create.Bar(new Node { Position = p8b }, new Node { Position = p5b });

            Bar bar9b = Create.Bar(new Node { Position = p1 }, new Node { Position = p5b });
            Bar bar10b = Create.Bar(new Node { Position = p2 }, new Node { Position = p6b });
            Bar bar11b = Create.Bar(new Node { Position = p3 }, new Node { Position = p7b });
            Bar bar12b = Create.Bar(new Node { Position = p4 }, new Node { Position = p8b });

            List<Bar> bars1 = new List<Bar>();
            List<Bar> bars2a = new List<Bar>();
            List<Bar> bars2b = new List<Bar>();

            bars1.Add(bar1);
            bars1.Add(bar2);
            bars1.Add(bar3);
            bars1.Add(bar4);

            bars2a.Add(bar5);
            bars2a.Add(bar6);
            bars2a.Add(bar7);
            bars2a.Add(bar8);
            bars2a.Add(bar9);
            bars2a.Add(bar10);
            bars2a.Add(bar11);
            bars2a.Add(bar12);

            bars2b.Add(bar5b);
            bars2b.Add(bar6b);
            bars2b.Add(bar7b);
            bars2b.Add(bar8b);
            bars2b.Add(bar9b);

            bars2b.Add(bar10b);
            bars2b.Add(bar11b);
            bars2b.Add(bar12b);

            ISectionProperty sec1a = new ExplicitSection();
            sec1a.Material = BH.Engine.Common.Create.Material("Material1", MaterialType.Concrete, 10, 10, 10, 10, 10);
            sec1a.Name = "Section 1";

            ISectionProperty sec1b = new ExplicitSection();
            sec1b.Material = BH.Engine.Common.Create.Material("Material1", MaterialType.Concrete, 10, 10, 10, 10, 10);
            sec1b.Name = "Section 1";

            ISectionProperty sec2 = new ExplicitSection();
            sec2.Material = BH.Engine.Common.Create.Material("Material2", MaterialType.Concrete, 10, 10, 10, 10, 10);
            sec2.Name = "Section 2";

            ISectionProperty sec3 = new ExplicitSection();
            sec3.Material = BH.Engine.Common.Create.Material("Material2", MaterialType.Concrete, 10, 10, 10, 10, 10);
            sec3.Name = "Section 3";

            foreach (Bar b in bars1.Concat(bars2a).Concat(bars2b))
            {
                b.SectionProperty = sec1a;
            }


            app.Push(nodesA, "Nodes");
            app.Push(nodesB, "Nodes");
            app.Push(bars1, "Bars1");
            app.Push(bars2a, "Bars2");
            app.Push(bars2b, "Bars2");

            Console.WriteLine("All Bars Pushed !");
            Console.ReadLine();
        }

        private static void TestPullBars(ETABSAdapter app)
        {

        }
    }
}
