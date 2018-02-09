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
            ISectionProperty sectionProperty = null;
            string materialName = "";

            switch (propertyType)
            {
                case eFramePropType.I:
                    
                    BH.oM.Structural.Properties.ShapeType.ISection

                case eFramePropType.Channel:
                    break;
                case eFramePropType.T:
                    break;
                case eFramePropType.Angle:
                    break;
                case eFramePropType.DblAngle:
                    break;
                case eFramePropType.Box:
                    break;
                case eFramePropType.Pipe:
                    break;
                case eFramePropType.Rectangular:
                    break;
                case eFramePropType.Circle:
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
                    break;
            }

            sectionProperty.Material = GetMaterial(model, materialName);
        }

    }
}
