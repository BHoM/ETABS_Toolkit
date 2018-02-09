using BH.oM.Structural.Properties;
using ETABS2016;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        private static ISectionProperty GetSectionProperty(cSapModel model, string propertyName, eFramePropType propertyType)
        {
            ISectionProperty bhSectionProperty = null;
            ISectionDimensions dimensions = null;
            string materialName = "";

            string fileName = "";
            double t3 = 0;
            double t2 = 0;
            double tf = 0;
            double tw = 0;
            double tfb = 0;
            double t2b = 0;

            int colour = 0;
            string notes = "";
            string guid = "";


            switch (propertyType)
            {
                case eFramePropType.I:
                    model.PropFrame.GetISection(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref t2b, ref tfb, ref colour, ref notes, ref guid);
                    if (false)//TODO: check if standard or fabricated
                        dimensions = new StandardISectionDimensions(t3, t2, tw, tf, 0, 0);
                    else
                        dimensions = new FabricatedISectionDimensions(t3, t2, t2b, tw, tf, tfb, 0);
                    break;
                case eFramePropType.Channel:
                    model.PropFrame.GetChannel(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    dimensions = new StandardChannelSectionDimensions(t3, t2, tw, tf, 0, 0);
                    break;
                case eFramePropType.T:
                    break;
                case eFramePropType.Angle:
                    break;
                case eFramePropType.DblAngle:
                    model.PropFrame.GetAngle(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    dimensions = new StandardAngleSectionDimensions(t3, t2, tw, tf, 0, 0);
                    break;
                case eFramePropType.Box:
                    model.PropFrame.GetTube(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref tf, ref tw, ref colour, ref notes, ref guid);
                    if (tf == tw)
                        dimensions = new StandardBoxDimensions(t3, t2, tf, 0, 0);
                    else
                        dimensions = new FabricatedBoxDimensions(t3, t2, tw, tf, tf, 0);
                    break;
                case eFramePropType.Pipe:
                    model.PropFrame.GetPipe(propertyName, ref fileName, ref materialName, ref t3, ref tw, ref colour, ref notes, ref guid);
                    dimensions = new TubeDimensions(t3, tw);
                    break;
                case eFramePropType.Rectangular:
                    model.PropFrame.GetRectangle(propertyName, ref fileName, ref materialName, ref t3, ref t2, ref colour, ref notes, ref guid);
                    dimensions = new RectangleSectionDimensions(t3, t2, 0);
                    break;
                case eFramePropType.Circle:
                    model.PropFrame.GetCircle(propertyName, ref fileName, ref materialName, ref t3, ref colour, ref notes, ref guid);
                    dimensions = new CircleDimensions(t3);
                    break;
                case eFramePropType.General:
                    break;
                case eFramePropType.DbChannel:
                    break;
                case eFramePropType.Auto:
                    break;
                case eFramePropType.SD:
                    break;
                case eFramePropType.Variable:
                    break;
                case eFramePropType.Joist:
                    break;
                case eFramePropType.Bridge:
                    break;
                case eFramePropType.Cold_C:
                    break;
                case eFramePropType.Cold_2C:
                    break;
                case eFramePropType.Cold_Z:
                    break;
                case eFramePropType.Cold_L:
                    break;
                case eFramePropType.Cold_2L:
                    break;
                case eFramePropType.Cold_Hat:
                    break;
                case eFramePropType.BuiltupICoverplate:
                    break;
                case eFramePropType.PCCGirderI:
                    break;
                case eFramePropType.PCCGirderU:
                    break;
                case eFramePropType.BuiltupIHybrid:
                    break;
                case eFramePropType.BuiltupUHybrid:
                    break;
                case eFramePropType.Concrete_L:
                    break;
                case eFramePropType.FilledTube:
                    break;
                case eFramePropType.FilledPipe:
                    break;
                case eFramePropType.EncasedRectangle:
                    break;
                case eFramePropType.EncasedCircle:
                    break;
                case eFramePropType.BucklingRestrainedBrace:
                    break;
                case eFramePropType.CoreBrace_BRB:
                    break;
                case eFramePropType.ConcreteTee:
                    break;
                case eFramePropType.ConcreteBox:
                    break;
                case eFramePropType.ConcretePipe:
                    break;
                case eFramePropType.ConcreteCross:
                    break;
                case eFramePropType.SteelPlate:
                    break;
                case eFramePropType.SteelRod:
                    break;
                default:
                    throw new NotImplementedException("Section convertion for the type: " + propertyType.ToString() + "is not implmented in ETABS adapter");
            }
            if(dimensions==null)
                throw new NotImplementedException("Section convertion for the type: " + propertyType.ToString() + "is not implmented in ETABS adapter");


            bhSectionProperty.Material = GetMaterial(model, materialName);

            return bhSectionProperty;
        }

    }
}
