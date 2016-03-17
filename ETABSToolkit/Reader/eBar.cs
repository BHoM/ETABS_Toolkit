using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ETABS2015;
using BHoM.Structural;
using BHoM.Structural.Results.Bars;

namespace ETABSToolkit.Reader
{
    public class eBar
    {
        /// <summary>
        /// default empty constructor - not sure this is the way to structure this .. maybe a static class
        /// </summary>
        public eBar() { }


        /// <summary>
        /// Get bar elements from ETABS instance and adds to a BHoM structure, the nodes defining the bars are added as well if they do not exist
        /// </summary>
        /// <param name="sapModel"></param>
        /// <param name="allBars">flase returns only the currently selected bars</param>
        /// <param name="structure"></param>
        /// <param name="barProperties"></param>
        public void GetBars(cSapModel sapModel, bool allBars, ref Structure structure, out List<BarProperty> barProperties)
        {
            //cSapModel sapModel = ETABSInstance.GetSapModel();
            //Dictionary<int, BHoM.Structural.Bar> structuralBars = new Dictionary<int, BHoM.Structural.Bar>();
            if (structure==null)
                structure = new BHoM.Structural.Structure();

            BHoM.Structural.Bar bar;
            BHoM.Structural.Node node1;
            BHoM.Structural.Node node2;
            Dictionary<string, BarProperty> loadedProperties = new Dictionary<string, BarProperty>();
            barProperties = new List<BarProperty>();

            int numberFrames = 0;
            bool selected = false;
            string[] names = null;
            string[] property = null;
            string[] story = null;
            string[] p1 = null;
            string[] p2 = null;
            double[] pX1 = null;
            double[] pX2 = null;
            double[] pY1 = null;
            double[] pY2 = null;
            double[] pZ1 = null;
            double[] pZ2 = null;
            double[] angle = null;
            double[] oX1 = null;
            double[] oX2 = null;
            double[] oY1 = null;
            double[] oY2 = null;
            double[] oZ1 = null;
            double[] oZ2 = null;
            int[] cardinalPoint = null;


            sapModel.FrameObj.GetAllFrames(ref numberFrames, ref names, ref property, ref story, ref p1, ref p2, ref pX1, ref pY1, ref pZ1, ref pX2, ref pY2, ref pZ2, ref angle, ref oX1, ref oX2, ref oY1, ref oY2, ref oZ1, ref oZ2, ref cardinalPoint);

            for (int i = 0; i < numberFrames; i++)
            {
                sapModel.FrameObj.GetSelected(names[i], ref selected);
                if (selected | allBars)
                {
                    node1 = new BHoM.Structural.Node(pX1[i], pY1[i], pZ1[i]);
                    node1.SetNumber(int.Parse(p1[i]));
                    node2 = new BHoM.Structural.Node(pX2[i], pY2[i], pZ2[i]);
                    node2.SetNumber(int.Parse(p2[i]));

                    bar = new BHoM.Structural.Bar(node1, node2);
                    bar.Number = int.Parse(names[i]);
                    //bar.SetStartNodeNumber(int.Parse(p1[i]));
                    //bar.SetEndNodeNumber(int.Parse(p2[i]));
                    //bar.Material = Utilities.GetMaterial(sapModel, matProperty);//existing material property
                    
                    node1.AddBar(bar);
                    node2.AddBar(bar);

                    if (!structure.NodeNumberClash(node1))
                        structure.AddOrGetNode(node1);

                    if (!structure.NodeNumberClash(node2))
                        structure.AddOrGetNode(node2);

                    BarProperty barProp = null;
                    if (!loadedProperties.TryGetValue(property[i], out barProp))
                    {
                        barProp = ETABSUtilities.GetBarProperty(sapModel, property[i]);
                        loadedProperties.Add(property[i], barProp);
                        bar.SectionPropertyName = barProp.Name;
                    }
                    barProperties.Add(barProp);
                    //attribs.Add(new BarAttributes(angle[i], new BarRelease(), new BarSupport()));

                    bar.OrientationPlane = ETABSUtilities.GetOrientationPlane(bar);

                    if (!structure.BarNumberClash(bar))
                        structure.AddBar(bar);

                }
            }

            //return structuralBars;
        }

        public Dictionary<int, List<BHoM.Structural.Results.Bars.BarForce>> GetBarResults(cSapModel sapModel, ref Structure structure, int samplePoints, List<BHoM.Structural.Loads.Loadcase> loadcases)
        {
            Dictionary<int, List<BarForce>> allBarForces = new Dictionary<int, List<BHoM.Structural.Results.Bars.BarForce>>(); // = new BHoM.Structural.Results.Bars.BarForce()

            //forces = new DataTree<Vector3d>();
            //moments = new DataTree<Vector3d>();
            samplePoints = samplePoints == 0 ? 2 : samplePoints;

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] objects = null;
            double[] ObjSta = null;
            string[] elm = null;
            double[] elmStar = null;
            string[] stepType = null;
            double[] stepNum = null;

            double[] fx = null;
            double[] fy = null;
            double[] fz = null;
            double[] mx = null;
            double[] my = null;
            double[] mz = null;

            sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();

            for (int i = 0; i < loadcases.Count; i++)
            {
                if (sapModel.Results.Setup.SetCaseSelectedForOutput(loadcases[i].Name) != 0)
                {
                    sapModel.Results.Setup.SetComboSelectedForOutput(loadcases[i].Name);
                }
            }

            Dictionary<int, BHoM.Structural.Bar> barDict = structure.Bars;

            foreach(KeyValuePair<int, BHoM.Structural.Bar> kvp in barDict)
            {
                sapModel.FrameObj.SetOutputStations(kvp.Key.ToString(), 2, 0, samplePoints);// -- not setting stations as expected. it is NOT certain that #stations = #samplepoints ! TODO
                
                int ret = sapModel.Results.FrameForce(kvp.Key.ToString(), eItemTypeElm.ObjectElm, ref resultCount, ref objects, ref ObjSta, ref elm, ref elmStar,
                ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz);

                BarForce barforce;
                List<BarForce> forcesOnCurrentBar = new List<BarForce>();

                BHoM.Geometry.Plane orientationPlane = ETABSUtilities.GetOrientationPlane(kvp.Value);
                BHoM.Geometry.Plane pl = new BHoM.Geometry.Plane();
                
                if (ret == 0)
                {
                    for (int lC = 0; lC < loadcases.Count; lC++) 
                    {
                        for (int sp = 0; sp < samplePoints; sp++)
                        {
                            int index = sp + lC * samplePoints;

                            double forceLocation = sp == 0 ? 0 : 1/sp;

                            barforce = new BarForce(kvp.Key, forceLocation, loadcases[lC], orientationPlane);//<--- the forceposition is uncertain !!!! TODO
                            barforce.FX = -fx[index];//this is likely needs to have reversed sign + = - !! TODO
                            barforce.FY = fy[index];
                            barforce.FZ = -fz[index];
                            barforce.MX = mx[index];
                            barforce.MY = my[index];
                            barforce.MZ = mz[index];

                            forcesOnCurrentBar.Add(barforce);
                        }
                    }

                    allBarForces.Add(kvp.Key, forcesOnCurrentBar);
                }
            }

            return allBarForces;
        }

    }

}
