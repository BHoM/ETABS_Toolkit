using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABS2015;
using BHoM.Structural.Elements;
using BHoM.Structural.Interface;
using BHoM.Base;
using Etabs_Adapter.Base;
using BHoM.Structural.Properties;
using Etabs_Adapter.Structural.Properties;

namespace Etabs_Adapter.Structural.Elements
{
    public class NodeIO
    {
        public static List<string> GetNodes(cOAPI Etabs, out List<Node> nodes, ObjectSelection selection, List<string> ids = null)
        {
            cSapModel SapModel = Etabs.SapModel;
            ObjectManager<string, Node> nodeManager = new ObjectManager<string, Node>(EtabsUtils.NUM_KEY, FilterOption.UserData);
            ObjectManager<NodeConstraint> constraintManager = new ObjectManager<NodeConstraint>();

            List<string> outIds = new List<string>();
            int numberFrames = 0;
            string[] names = null;
            double[] pX = null;
            double[] pY = null;
            double[] pZ = null;
            bool[] restraint = null;
            double[] springValues = null;
            bool selected = false;

            if (selection != ObjectSelection.FromInput)
            {
                SapModel.PointObj.GetAllPoints(ref numberFrames, ref names, ref pX, ref pY, ref pZ);

                for (int i = 0; i < numberFrames; i++)
                {
                    if (selection == ObjectSelection.Selected)
                    {
                        SapModel.PointObj.GetSelected(names[i], ref selected);
                        if (!selected) continue;
                    }

                    nodeManager.Add(names[i], new Node(pX[i], pY[i], pZ[i]));
                    outIds.Add(names[i]);
                }             
            }
            else
            {
                double X = 0;
                double Y = 0;
                double Z = 0;

                for (int i = 0; ids != null && i < ids.Count; i++)
                {
                    if (SapModel.PointObj.GetCoordCartesian(ids[i], ref X, ref Y, ref Z) == 0)
                    {
                        outIds.Add(ids[i]);
                        nodeManager.Add(ids[i], new Node(X, Y, Z));
                    }
                }
            }

            for (int i = 0; i < outIds.Count; i++)
            {
                if (SapModel.PointObj.GetRestraint(outIds[i], ref restraint) != 0) restraint = null;
                if (SapModel.PointObj.GetSpring(outIds[i], ref springValues) != 0) springValues = null;

                if (!IsFree(restraint) || ContainsSpring(springValues))
                {
                    NodeConstraint c = PropertyIO.GetNodeConstraint(restraint, springValues);
                    constraintManager.Add(c.Name, c);
                    nodeManager[outIds[i]].Constraint = constraintManager[c.Name];
                }
            }

            nodes = nodeManager.GetRange(outIds);
            return outIds;
        }

        public static bool IsFree(bool[] restraint)
        {
            if (restraint == null) return true;
            for (int i = 0; i < restraint.Length; i++)
            {
                if (restraint[i] == true) return false;
            }
            return true;
        }

        public static bool ContainsSpring(double[] value)
        {
            if (value == null) return false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) return true;
            }
            return false;
        }

        public static bool CreateNodes(cOAPI Etabs, List<Node> nodes, out List<string> ids)
        {
            cSapModel SapModel = Etabs.SapModel;
            ids = new List<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                string name = "";
                SapModel.PointObj.AddCartesian(nodes[i].X, nodes[i].Y, nodes[i].Z, ref name);
                if (nodes[i].CustomData.ContainsKey(EtabsUtils.NUM_KEY))
                {
                    nodes[i].CustomData[EtabsUtils.NUM_KEY] = name;
                }
                else
                {
                    nodes[i].CustomData.Add(EtabsUtils.NUM_KEY, name);
                }
                if (nodes[i].Constraint != null)
                {
                    bool[] restraint = new bool[6];
                    restraint[0] = nodes[i].Constraint.UX == DOFType.Fixed;
                    restraint[1] = nodes[i].Constraint.UY == DOFType.Fixed;
                    restraint[2] = nodes[i].Constraint.UZ == DOFType.Fixed;
                    restraint[3] = nodes[i].Constraint.RX == DOFType.Fixed;
                    restraint[4] = nodes[i].Constraint.RY == DOFType.Fixed;
                    restraint[5] = nodes[i].Constraint.RZ == DOFType.Fixed;

                    double[] spring = new double[6];
                    spring[0] = nodes[i].Constraint.KX;
                    spring[1] = nodes[i].Constraint.KY;
                    spring[2] = nodes[i].Constraint.KZ;
                    spring[3] = nodes[i].Constraint.HX;
                    spring[4] = nodes[i].Constraint.HY;
                    spring[5] = nodes[i].Constraint.HZ;

                    SapModel.PointObj.SetRestraint(name, ref restraint);
                    SapModel.PointObj.SetSpring(name, ref spring);
                }
                ids.Add(name);
            }
            return true;

        }
    }
}
