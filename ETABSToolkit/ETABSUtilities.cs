using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BHoM;
using BHoM.Structural.SectionProperties;
using ETABS2015;
using BHoM.Structural;

namespace ETABSToolkit
{
    public class ETABSUtilities
    {
        public static BarProperty GetBarProperty(cSapModel sapModel, string name)
        {
            eFramePropType type = eFramePropType.I;
            sapModel.PropFrame.GetTypeOAPI(name, ref type);
            string fileName = "";
            double t3 = 0;//diameter or depth
            double t2 = 0;//width
            double tf = 0;//flange thickness
            double tw = 0;//wall or web thickness
            double tfb = 0;//thickness of bottom flange
            double t2b = 0;//width of bottom flange

            string eMaterial = "";
            int colour = 0;
            string notes = "";
            string guid = "";

            ShapeType outType = ShapeType.SteelCircularHollow;

            switch (type)
            {
                case eFramePropType.I:
                    sapModel.PropFrame.GetISection(name, ref fileName, ref eMaterial, ref t3, ref t2, ref tf, ref tw, ref t2b, ref tfb, ref colour, ref notes, ref guid);
                    outType = ShapeType.SteelI;
                    break;
                //case eFramePropType.Box:
                //    sapModel.PropFrame.GetTube(name, ref fileName, ref eMaterial, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                //    outType = SectionType.Box;
                //    break;
                case eFramePropType.Circle:
                    sapModel.PropFrame.GetCircle(name, ref fileName, ref eMaterial, ref t3, ref colour, ref notes, ref guid);
                    outType = ShapeType.SteelCircularHollow;
                    break;
                case eFramePropType.Pipe:
                    sapModel.PropFrame.GetPipe(name, ref fileName, ref eMaterial, ref t3, ref tf, ref colour, ref notes, ref guid);
                    outType = ShapeType.SteelCircularHollow;
                    break;
                case eFramePropType.Rectangular:
                    sapModel.PropFrame.GetRectangle(name, ref fileName, ref eMaterial, ref t3, ref t2, ref colour, ref notes, ref guid);
                    outType = ShapeType.SteelRectangularHollow;
                    break;
                default:
                    sapModel.PropFrame.GetPipe(name, ref fileName, ref eMaterial, ref t3, ref tf, ref colour, ref notes, ref guid);
                    outType = ShapeType.SteelCircularHollow;
                    break;

                    //case eFramePropType.Angle:
                    //    sapModel.PropFrame.GetAngle(name, ref fileName, ref eMaterial, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    //    outType = SectionType.Angle;
                    //    break;
            }

            BHoM.Materials.Material material = GetMaterial(sapModel, name);
            Section sectionProperty = new Section(name, outType, t3, t2, tf, tw);
            sectionProperty.Material = material;
            BarProperty barProperty = new BarProperty(name);
            barProperty.Material = material;
            barProperty.Section = sectionProperty;

            return barProperty;
        }

        public static BHoM.Materials.Material GetMaterial(cSapModel sapModel, string name)
        {
            //BHoM.Materials.Material material = new BHoM.Materials.Material()

            eMatType matType = eMatType.NoDesign;
            int colour = 0;
            string guid = "";
            string notes = "";
            if (sapModel.PropMaterial.GetMaterial(name, ref matType, ref colour, ref notes, ref guid) != 0)
            {
                int index = 0; //TODO: find a way to set this
                double e = 0;
                double v = 0;
                double thermCo = 0;
                double g = 0;
                double mass = 0;
                double weight = 0;
                sapModel.PropMaterial.GetMPIsotropic(name, ref e, ref v, ref thermCo, ref g);
                sapModel.PropMaterial.GetWeightAndMass(name, ref weight, ref mass);

                BHoM.Materials.Material material = new BHoM.Materials.Material(index, name, e, v, g, mass, thermCo);

                return material;
            }
            return null;
        }

        public static void GetLoadcases(cSapModel sapModel, out List<BHoM.Structural.Loads.Loadcase> cases)
        {

            cases = new List<BHoM.Structural.Loads.Loadcase>();

            int numNames = 0;
            string[] names = null;
            //loadcases
            sapModel.LoadCases.GetNameList(ref numNames, ref names);
            for (int i = 0; i < numNames; i++)
            {
                //skip the loadcases that for some reason is not a loadcase when getting bar forces
                if (!names[i].StartsWith("~"))
                {
                    cases.Add(new BHoM.Structural.Loads.Loadcase(i, names[i]));
                }
            }
            ////load combinations
            //sapModel.RespCombo.GetNameList(ref numNames, ref names);
            //for (int i = 0; i < numNames; i++)
            //{
            //    cases.Add(new BHoM.Structural.Loads.Loadcase(numNames, names[i]));
            //}
        }

        /// <summary>
        /// returns the default orientation plane set by ETABS - this does NOT account for any local axis customisations done in ETABS
        /// </summary>
        /// <param name="bar"></param>
        /// <returns></returns>
        public static BHoM.Geometry.Plane GetOrientationPlane(BHoM.Structural.Bar bar)
        {
            // for reference see: http://docs.csiamerica.com/help-files/etabs/Menus/Assign/Frame/Local_Axes_Frames.htm
            //NOTE: 1st=X,2nd=Z,3rd=Y

            BHoM.Structural.Node nodeA = bar.StartNode;
            BHoM.Structural.Node nodeB = bar.EndNode;
            BHoM.Geometry.Plane orientationPlane;
            BHoM.Geometry.Vector xVector;
            BHoM.Geometry.Vector yVector;

            //bar is a ETABS beam - horisontal
            if (nodeA.Z == nodeB.Z)
            {
                if (nodeA.X == nodeB.X)//y-axis is determining
                {
                    if (nodeA.Y < nodeB.Y)
                        xVector = new BHoM.Geometry.Vector(nodeA.Point, nodeB.Point);//vector from A to B
                    else
                        xVector = new BHoM.Geometry.Vector(nodeB.Point, nodeA.Point);//vector fromB to A
                }
                else
                {
                    if (nodeA.X < nodeB.X)
                        xVector = new BHoM.Geometry.Vector(nodeA.Point, nodeB.Point);//vector from A to B
                    else
                        xVector = new BHoM.Geometry.Vector(nodeB.Point, nodeA.Point);//vector fromB to A
                }
                yVector = BHoM.Geometry.Vector.CrossProduct(xVector, new BHoM.Geometry.Vector(0, 0, 1));
                orientationPlane = new BHoM.Geometry.Plane(xVector, yVector, nodeA.Point);
                return orientationPlane;
            }

            //bar is an ETABS column - vertical
            if (nodeA.X == nodeB.X & nodeA.Y == nodeB.Y)
            {
                xVector = new BHoM.Geometry.Vector(0, 0, 1);
                yVector = BHoM.Geometry.Vector.CrossProduct(new BHoM.Geometry.Vector(1, 0, 0), xVector);
                orientationPlane = new BHoM.Geometry.Plane(xVector, yVector, nodeA.Point);
                return orientationPlane;
            }

            //bar is an ETABS brace - 
            if (nodeA.Z != nodeB.Z)
            {
                if (nodeA.Z < nodeB.Z)
                    xVector = new BHoM.Geometry.Vector(nodeA.Point, nodeB.Point);//vector from A to B
                else
                    xVector = new BHoM.Geometry.Vector(nodeB.Point, nodeA.Point);//vector fromB to A

                 //BHoM.Geometry.Vector perpVector = BHoM.Geometry.Vector.CrossProduct(xVector, new BHoM.Geometry.Vector(0, 0, 1));
                 yVector = BHoM.Geometry.Vector.CrossProduct(new BHoM.Geometry.Vector(0, 0, 1), xVector);
                orientationPlane = new BHoM.Geometry.Plane(xVector, yVector, nodeA.Point);
                return orientationPlane;
            }

            //something was wrong
            return null;
        }

        public BHoM.Materials.MaterialType GetMaterialType(eMatType materialType)
        {
            switch (materialType)
            {
                case eMatType.Concrete:
                    return BHoM.Materials.MaterialType.Concrete;
                case eMatType.Steel:
                    return BHoM.Materials.MaterialType.Steel;
                case eMatType.NoDesign:
                    return BHoM.Materials.MaterialType.Timber;
                case eMatType.Aluminum:
                    return BHoM.Materials.MaterialType.Aluminium;
                default:
                    return BHoM.Materials.MaterialType.Steel;
            }
        }

    }


    public class BarProperty
    {
        string _name;
        Section _section;
        BHoM.Materials.Material _material;

        
        public BarProperty(string name)
        {
            _name = name;
            _section = new Section();
            _material = new BHoM.Materials.Material("Unset");
        }

        public BarProperty()
        {
            _name = "Unset";
            _section = new Section();
            _material = new BHoM.Materials.Material("Unset");
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public Section Section
        {
            get { return _section; }
            set { _section = value; }
        }
        //public BHoM.Structural.SectionProperties.ISectionProperty Section
        //{
        //    get { return _section; }
        //    set { _section = value; }
        //}

        public BHoM.Materials.Material Material
        {
            get { return _material; }
            set { _material = value; }
        }
    }

    public class Section : SectionProperty
    {
        double _t3 = 0;//diameter or depth
        double _t2 = 0;//width
        double _tf = 0;//flange thickness
        double _tw = 0;//wall or web thickness
        double _tfb = 0;//thickness of bottom flange
        double _t2b = 0;//width of bottom flange

        public Section()
        {

        }

        public Section(string name, ShapeType type, double t3, double t2, double tf = 0, double tw = 0)
        {
            Name = name;
            base.Type = type;
            _t2 = t2;
            _t3 = t3;
            _tf = tf;
            _tw = tw;
        }
    }

    ///// <summary>
    ///// this really should be from the BHoM, but there are just to many changes going on there at the moment
    ///// </summary>
    //public class Section
    //{

    //    public string Name { get; set; }
    //    public ShapeType shape { get; set; }
    //    public double Width { get; set; }
    //    public double Depth { get; set; }
    //    public double Tf { get; set; }
    //    public double Tw { get; set; }
    //    public double Ri { get; set; }
    //    public double Ro { get; set; }
    //    public double Spacing { get; set; }
    //    public double Rotation { get; set; }

    //    public Section()
    //    {
    //        Name = "Unset";
    //    }

    //    public Section(string name, ShapeType type, double width, double depth, double t1 = 0, double t2 = 0, double r1 = 0, double r2 = 0, double s = 0, double rotation = 0)
    //    {
    //        Name = name;
    //        shape = type;
    //        Width = width;
    //        Depth = depth;
    //        Tf = t1;
    //        Tw = t2;
    //        Ri = r1;
    //        Ro = r2;
    //        Spacing = s;
    //    }

}

