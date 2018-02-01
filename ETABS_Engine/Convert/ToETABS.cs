using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABS2016;
using BH.oM;
using BH.oM.Structural;
using BH.oM.Structural.Elements;
using BH.oM.Structural.Properties;
using BH.Engine.Serialiser;

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        public static void ToETABS(this BH.oM.Base.IObject obj, cSapModel model)
        {
            Convert.ToETABS(obj as dynamic, model);
        }

        public static void ToETABS(this Node bhNode, cSapModel model)
        {
            // Note: 'name' is the only editable text field and might need to be reserved for the Tags
            // 'name' is displayed in the UI as 'unique name' and is editable, 'lable' is also visible but not editable
            // all objects are created in the API with specifying the 'name' - this is the natural place to store the object identifier (CustomObject[adapterId])
            // we also need to store tags somewhere and the only other suitable field is 'GUID' which is stored as string type on the objects in ETABS
            // as long as tags are unique it should be ok to store tags here - it looks like all elements have get/set for GUID.
            // 'name' and 'lable' (non editable from UI and API) 

            string name = bhNode.CustomData[AdapterId].ToString();
            model.PointObj.AddCartesian(bhNode.Position.X, bhNode.Position.Y, bhNode.Position.Z, ref name);

            //TODO: update the BHOM Node with the id acctually assigned in ETABS, or don't - not sure which would best align with behaviour of other adapters
            //bhNode.CustomData[AdapterId] = name;

            bool[] restraint = new bool[6];
            restraint[0] = bhNode.Constraint.TranslationX == DOFType.Fixed;
            restraint[1] = bhNode.Constraint.TranslationY == DOFType.Fixed;
            restraint[2] = bhNode.Constraint.TranslationZ == DOFType.Fixed;
            restraint[3] = bhNode.Constraint.RotationX == DOFType.Fixed;
            restraint[4] = bhNode.Constraint.RotationY == DOFType.Fixed;
            restraint[5] = bhNode.Constraint.RotationZ == DOFType.Fixed;

            double[] spring = new double[6];
            spring[0] = bhNode.Constraint.TranslationalStiffnessX;
            spring[1] = bhNode.Constraint.TranslationalStiffnessY;
            spring[2] = bhNode.Constraint.TranslationalStiffnessZ;
            spring[3] = bhNode.Constraint.RotationalStiffnessX;
            spring[4] = bhNode.Constraint.RotationalStiffnessY;
            spring[5] = bhNode.Constraint.RotationalStiffnessZ;

            model.PointObj.SetRestraint(name, ref restraint);
            model.PointObj.SetSpring(name, ref spring);

            ////it is likely that the tags used needs to be unique to work in etabs - add object id or guid to the tag hashset?
            //model.PointObj.SetGUID(name, bhNode.TaggedName());

        }

        public static void ToETABS(this Bar bhBar, cSapModel model)
        {
            // remember to ensure the nodes for the bar are created first!
            bhBar.StartNode.CustomData[]

            model.FrameObj.
        }
    }
}
