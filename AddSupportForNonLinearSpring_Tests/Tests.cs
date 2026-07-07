using BH.Adapter.ETABS;
using BH.oM.Adapters;
using BH.oM.Adapters.ETABS;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using BH.oM.Structure.Springs;
using BH.oM.Adapter;
using BH.oM.Data.Requests;
using System.Linq;
using BH.oM.Structure.Elements;
using BH.oM.Geometry;
using BH.oM.Structure.Constraints;
using BH.Engine.Base;
using BH.oM.Adapters.ETABS.Fragments;
using BH.Engine.Adapters.ETABS;
using BH.oM.Structure.Springs.NonLinearBehaviour;

namespace AddSupportForNonLinearSpring_Tests
{
    public class Tests
    {
        private ETABSAdapter m_Adapter;
        private const string etabsProject = @"C:\Users\asigurdsson\OneDrive - Buro Happold\Documents\ETABS development\TestModels\PushModel.EDB";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            EtabsSettings etabsSettings = new EtabsSettings();
            etabsSettings.EtabsVersion = EtabsVersion.v22;

            try
            {
                m_Adapter = new ETABSAdapter(filePath: etabsProject, etabsSetting: etabsSettings, active: true);

            }
            catch(Exception ex)
            {
                Assert.Ignore($"Could not connect to ETABS: {ex.Message}");
            }

            Assert.That(m_Adapter, Is.Not.Null, "Adapter was not created.");
        }

        [Test, Order(1)]
        public void PushElasticSpringPropertyToETABS()
        {
            ForceDeformationCurves forceDeformationCurves = new ForceDeformationCurves
            {
                TranslationX = new List<ForceDeformationPoint>
                {
                    new ForceDeformationPoint
                    {
                        Deformation = -0.1,
                        Force = -5000
                    },
                    new ForceDeformationPoint
                    {
                        Deformation = 0,
                        Force = 0
                    },
                    new ForceDeformationPoint
                    {
                        Deformation = 0.1,
                        Force = 5000
                    }
                }
            };

            PointSpringProperty pointSpring = new PointSpringProperty
            {
                Name = "TestElasticPointSpring",

                TranslationalStiffnessX = 5000,
                TranslationalStiffnessY = 5000,
                TranslationalStiffnessZ = 5000,

                TranslationX = DOFType.Free,
                TranslationY = DOFType.Fixed,
                TranslationZ = DOFType.Fixed,

                NonlinearBehaviour = new MultiLinearElasticBehaviour
                {
                    ForceDeformationCurves = forceDeformationCurves
                }
            };


            List<object> pushed = m_Adapter.Push(
                new List<ISpringProperty> { pointSpring },
                pushType: PushType.CreateOnly);

            Assert.That(pushed.Count, Is.EqualTo(1), "Node was not pushed.");
        }

        [Test, Order(2)]
        public void PushPlasticSpringPropertyToETABS()
        {
            ForceDeformationCurves forceDeformationCurves = new ForceDeformationCurves
            {
                TranslationX = new List<ForceDeformationPoint>
                {
                    new ForceDeformationPoint
                    {
                        Deformation = -0.1,
                        Force = -5000
                    },
                    new ForceDeformationPoint
                    {
                        Deformation = 0,
                        Force = 0
                    },
                    new ForceDeformationPoint
                    {
                        Deformation = 0.1,
                        Force = 5000
                    }
                }
            };

            PointSpringProperty pointSpring = new PointSpringProperty
            {
                Name = "TestPlasticPointSpring",

                TranslationalStiffnessX = 5000,

                NonlinearBehaviour = new MultiLinearPlasticBehaviour
                {
                    ForceDeformationCurves = forceDeformationCurves
                }
            };
            

            List<object> pushed = m_Adapter.Push(
                new List<ISpringProperty> { pointSpring },
                pushType: PushType.CreateOnly);

            Assert.That(pushed.Count, Is.EqualTo(1), "Node was not pushed.");
        }

        [Test, Order(3)]
        public void PushGapSpringPropertyToETABS()
        {

            PointSpringProperty pointSpring = new PointSpringProperty
            {
                Name = "TestGapPointSpring",

                TranslationalStiffnessX = 5000,

                NonlinearBehaviour = new GapBehaviour
                {
                    InitialStiffness = new NonlinearSpringValues
                    {
                        TranslationX = 500
                    },
                    InitialOpening = new NonlinearSpringValues
                    {
                        TranslationX = 500
                    }
                }


            };

            List<object> pushed = m_Adapter.Push(
                new List<ISpringProperty> { pointSpring },
                pushType: PushType.CreateOnly);

            Assert.That(pushed.Count, Is.EqualTo(1), "Node was not pushed.");
        }

        /***************************************************/
        /**** Round-trip tests (push then pull)         ****/
        /***************************************************/

        // Pulls every PointSpringProperty back from ETABS and returns the one with the given name.
        private PointSpringProperty PullSpringByName(string name)
        {
            FilterRequest request = new FilterRequest { Type = typeof(PointSpringProperty) };
            return m_Adapter.Pull(request)
                .OfType<PointSpringProperty>()
                .FirstOrDefault(s => s.Name == name);
        }

        [Test, Order(10)]
        public void RoundTripGapSpringProperty()
        {
            PointSpringProperty pointSpring = new PointSpringProperty
            {
                Name = "RT_GapPointSpring",
                TranslationalStiffnessX = 5000,
                NonlinearBehaviour = new GapBehaviour
                {
                    InitialStiffness = new NonlinearSpringValues { TranslationX = 750 },
                    InitialOpening = new NonlinearSpringValues { TranslationX = 0.01 }
                }
            };

            m_Adapter.Push(new List<ISpringProperty> { pointSpring }, pushType: PushType.CreateOnly);

            PointSpringProperty pulled = PullSpringByName("RT_GapPointSpring");

            Assert.That(pulled, Is.Not.Null, "Spring was not pulled back.");
            Assert.That(pulled.NonlinearBehaviour, Is.TypeOf<GapBehaviour>(),
                "Nonlinear behaviour did not round-trip - the link was not attached to the point spring property.");

            GapBehaviour gap = (GapBehaviour)pulled.NonlinearBehaviour;
            Assert.That(gap.InitialStiffness.TranslationX, Is.EqualTo(750).Within(1e-3), "Gap initial stiffness did not round-trip.");
            Assert.That(gap.InitialOpening.TranslationX, Is.EqualTo(0.01).Within(1e-6), "Gap initial opening did not round-trip.");
            Assert.That(pulled.TranslationalStiffnessX, Is.EqualTo(5000).Within(1e-3), "Effective stiffness (Ke) did not round-trip.");
        }

        [Test, Order(11)]
        public void RoundTripHookSpringProperty()
        {
            PointSpringProperty pointSpring = new PointSpringProperty
            {
                Name = "RT_HookPointSpring",
                TranslationalStiffnessX = 5000,
                NonlinearBehaviour = new HookBehaviour
                {
                    InitialStiffness = new NonlinearSpringValues { TranslationX = 750 },
                    InitialOpening = new NonlinearSpringValues { TranslationX = 0.02 }
                }
            };

            m_Adapter.Push(new List<ISpringProperty> { pointSpring }, pushType: PushType.CreateOnly);

            PointSpringProperty pulled = PullSpringByName("RT_HookPointSpring");

            Assert.That(pulled, Is.Not.Null, "Spring was not pulled back.");
            Assert.That(pulled.NonlinearBehaviour, Is.TypeOf<HookBehaviour>(),
                "Nonlinear behaviour did not round-trip - the link was not attached to the point spring property.");

            HookBehaviour hook = (HookBehaviour)pulled.NonlinearBehaviour;
            Assert.That(hook.InitialStiffness.TranslationX, Is.EqualTo(750).Within(1e-3), "Hook initial stiffness did not round-trip.");
            Assert.That(hook.InitialOpening.TranslationX, Is.EqualTo(0.02).Within(1e-6), "Hook initial opening did not round-trip.");
        }

        [Test, Order(12)]
        public void RoundTripDamperSpringProperty()
        {
            PointSpringProperty pointSpring = new PointSpringProperty
            {
                Name = "RT_DamperPointSpring",
                TranslationalStiffnessX = 5000,
                NonlinearBehaviour = new DamperBehaviour
                {
                    InitialStiffness = new NonlinearSpringValues { TranslationX = 750 },
                    DampingCoefficient = new NonlinearSpringValues { TranslationX = 100 },
                    DampingExponent = new NonlinearSpringValues { TranslationX = 1.0 }
                }
            };

            m_Adapter.Push(new List<ISpringProperty> { pointSpring }, pushType: PushType.CreateOnly);

            PointSpringProperty pulled = PullSpringByName("RT_DamperPointSpring");

            Assert.That(pulled, Is.Not.Null, "Spring was not pulled back.");
            Assert.That(pulled.NonlinearBehaviour, Is.TypeOf<DamperBehaviour>(),
                "Nonlinear behaviour did not round-trip - the link was not attached to the point spring property.");

            DamperBehaviour damper = (DamperBehaviour)pulled.NonlinearBehaviour;
            Assert.That(damper.DampingCoefficient.TranslationX, Is.EqualTo(100).Within(1e-3), "Damper coefficient did not round-trip.");
            Assert.That(damper.DampingExponent.TranslationX, Is.EqualTo(1.0).Within(1e-6), "Damper exponent did not round-trip.");
        }

        [Test, Order(13)]
        public void RoundTripMultiLinearElasticSpringProperty()
        {
            ForceDeformationCurves curves = new ForceDeformationCurves
            {
                TranslationX = new List<ForceDeformationPoint>
                {
                    new ForceDeformationPoint { Deformation = -0.1, Force = -5000 },
                    new ForceDeformationPoint { Deformation = 0, Force = 0 },
                    new ForceDeformationPoint { Deformation = 0.1, Force = 5000 }
                }
            };

            PointSpringProperty pointSpring = new PointSpringProperty
            {
                Name = "RT_ElasticPointSpring",
                TranslationalStiffnessX = 5000,
                NonlinearBehaviour = new MultiLinearElasticBehaviour { ForceDeformationCurves = curves }
            };

            m_Adapter.Push(new List<ISpringProperty> { pointSpring }, pushType: PushType.CreateOnly);

            PointSpringProperty pulled = PullSpringByName("RT_ElasticPointSpring");

            Assert.That(pulled, Is.Not.Null, "Spring was not pulled back.");
            Assert.That(pulled.NonlinearBehaviour, Is.TypeOf<MultiLinearElasticBehaviour>(),
                "Nonlinear behaviour did not round-trip - the link was not attached to the point spring property.");

            MultiLinearElasticBehaviour ml = (MultiLinearElasticBehaviour)pulled.NonlinearBehaviour;
            Assert.That(ml.ForceDeformationCurves.TranslationX, Is.Not.Null.And.Count.EqualTo(3),
                "Force-deformation curve did not round-trip.");
        }

        /***************************************************/
        /**** Node <-> PointSpringProperty support      ****/
        /***************************************************/

        // Pulls every Node back from ETABS and returns the one with the given name.
        private Node PullNodeByName(string name)
        {
            FilterRequest request = new FilterRequest { Type = typeof(Node) };
            return m_Adapter.Pull(request)
                .OfType<Node>()
                .FirstOrDefault(n => n.Name == name);
        }

        [Test, Order(20)]
        public void PushNodeWithNonlinearSpringSupportCreatesAndAssigns()
        {
            PointSpringProperty support = new PointSpringProperty
            {
                Name = "NodeSupportGap",
                TranslationalStiffnessX = 5000,
                NonlinearBehaviour = new GapBehaviour
                {
                    InitialStiffness = new NonlinearSpringValues { TranslationX = 750 },
                    InitialOpening = new NonlinearSpringValues { TranslationX = 0.01 }
                }
            };

            Node node = new Node
            {
                Name = "NodeWithGapSupport",
                Position = new Point { X = 13, Y = 7, Z = 0 },
                Support = support
            };

            m_Adapter.Push(new List<Node> { node }, pushType: PushType.CreateOnly);

            // The node push should have created the spring property in ETABS...
            PointSpringProperty pulledSpring = PullSpringByName("NodeSupportGap");
            Assert.That(pulledSpring, Is.Not.Null, "Spring property was not created by the node push.");
            Assert.That(pulledSpring.NonlinearBehaviour, Is.TypeOf<GapBehaviour>());

            // ...and assigned it to the point, so the node pulls back with a PointSpringProperty support.
            Node pulledNode = PullNodeByName("NodeWithGapSupport");
            Assert.That(pulledNode, Is.Not.Null, "Node was not pulled back.");
            Assert.That(pulledNode.Support, Is.TypeOf<PointSpringProperty>(),
                "Node support did not round-trip as a PointSpringProperty - the spring was not assigned/read.");

            PointSpringProperty nodeSupport = (PointSpringProperty)pulledNode.Support;
            Assert.That(nodeSupport.Name, Is.EqualTo("NodeSupportGap"));
            Assert.That(nodeSupport.NonlinearBehaviour, Is.TypeOf<GapBehaviour>(),
                "Nonlinear behaviour did not round-trip onto the node support.");
        }

        [Test, Order(21)]
        public void UpdateNodeToDifferentSpringSupport()
        {
            // A second spring property, pushed on its own.
            PointSpringProperty support2 = new PointSpringProperty
            {
                Name = "NodeSupportGap2",
                TranslationalStiffnessX = 8000,
                NonlinearBehaviour = new GapBehaviour
                {
                    InitialStiffness = new NonlinearSpringValues { TranslationX = 1200 },
                    InitialOpening = new NonlinearSpringValues { TranslationX = 0.02 }
                }
            };
            m_Adapter.Push(new List<ISpringProperty> { support2 }, pushType: PushType.CreateOnly);

            // Pull the node from the previous test (carries its adapter id), re-point its support to the new
            // property by name, and push an update.
            Node node = PullNodeByName("NodeWithGapSupport");
            Assert.That(node, Is.Not.Null, "Precondition: the node from the create test should exist.");
            Assert.That(node.Support, Is.Not.Null);

            node.Support.Name = "NodeSupportGap2";
            m_Adapter.Push(new List<Node> { node }, pushType: PushType.UpdateOnly);

            Node pulledNode = PullNodeByName("NodeWithGapSupport");
            Assert.That(pulledNode, Is.Not.Null);
            Assert.That(pulledNode.Support, Is.Not.Null);
            Assert.That(pulledNode.Support.Name, Is.EqualTo("NodeSupportGap2"),
                "Node support was not re-pointed to the new spring property.");
        }
    }
}