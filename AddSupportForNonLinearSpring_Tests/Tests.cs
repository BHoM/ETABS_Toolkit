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
        public void PushNonLinearSpringToETABS()
        {
            NonLinearSpring nonLinearSpring = new NonLinearSpring
            {
                TranslationalStiffnessX = 5000,
                TranslationalStiffnessY = 5000,
                TranslationalStiffnessZ = 5000,
                RotationalStiffnessX = 100,
                RotationalStiffnessY = 100,
                RotationalStiffnessZ = 100,

                ForceDeformationCurves = new ForceDeformationCurves
                {
                    TranslationX = new List<ForceDeformationPoint>
                    {
                        new ForceDeformationPoint { Deformation = -0.10, Force = -5000 },
                        new ForceDeformationPoint { Deformation =  0.00, Force =     0 },
                        new ForceDeformationPoint { Deformation =  0.10, Force =  5000 }
                    },


                    TranslationY = new List<ForceDeformationPoint>
                    {
                        new ForceDeformationPoint { Deformation = -0.10, Force = -5000 },
                        new ForceDeformationPoint { Deformation =  0.00, Force =     0 },
                        new ForceDeformationPoint { Deformation =  0.10, Force =  5000 }
                    },

                    TranslationZ = new List<ForceDeformationPoint>
                    {
                        new ForceDeformationPoint { Deformation = -0.10, Force = -5000 },
                        new ForceDeformationPoint { Deformation =  0.00, Force =     0 },
                        new ForceDeformationPoint { Deformation =  0.10, Force =  5000 }
                    },

                    RotationX = new List<ForceDeformationPoint>
                    {
                        new ForceDeformationPoint { Deformation = -0.10, Force = -5000 },
                        new ForceDeformationPoint { Deformation =  0.00, Force =     0 },
                        new ForceDeformationPoint { Deformation =  0.10, Force =  5000 }
                    }
                }
            };

            nonLinearSpring = nonLinearSpring.SetNonLinearSpringProperties(NonLinearSpringType.MultiLinearElastic, NonLinearSpringHysteresisType.Kinematic);

            Node node = new Node
            {
                Position = new Point { X = 0, Y = 0, Z = 3 },
                NonLinearSpring = nonLinearSpring
            };

            List<object> pushed = m_Adapter.Push(
                new List<Node> { node },
                pushType: PushType.CreateOnly);

            Assert.That(pushed.Count, Is.EqualTo(1), "Node was not pushed.");
        }

        [Test, Order(2)]
        public void PullNonLinearSpringFromETABS()
        {

            FilterRequest filterRequest = new FilterRequest()
            {
                Type = typeof(Node)
            };

            List<object> pulled = m_Adapter.Pull(request: filterRequest).ToList();
            Node nodePulled = (Node)pulled[0];

            NonLinearSpring spring = nodePulled.NonLinearSpring;

            if (spring == null)
            {
                TestContext.WriteLine("No NonLinearSpring on this node.");
                return;
            }

            foreach (ForceDeformationPoint point in spring.ForceDeformationCurves.TranslationX)
                TestContext.WriteLine($"X - Deformation: {point.Deformation}, Force: {point.Force}");

            foreach (ForceDeformationPoint point in spring.ForceDeformationCurves.TranslationY)
                TestContext.WriteLine($"Y - Deformation: {point.Deformation}, Force: {point.Force}");

            foreach (ForceDeformationPoint point in spring.ForceDeformationCurves.TranslationZ)
                TestContext.WriteLine($"Z - Deformation: {point.Deformation}, Force: {point.Force}");

            Assert.That(pulled, Is.Not.Null);

        }
    }
}