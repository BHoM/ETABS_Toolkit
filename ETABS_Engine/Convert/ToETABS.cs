﻿using System;
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

        public static string ToETABS(this Node bhNode, cSapModel model)
        {
            // Note: 'name' is the only editable text field and might need to be reserved for the Tags
            // 'name' is displayed in the UI as 'unique name' and is editable, 'lable' is also visible but not editable
            // all objects are created in the API with specifying the 'name' - this is the natural place to store the object identifier (CustomObject[adapterId])
            // we also need to store tags somewhere and the only other suitable field is 'GUID' which is stored as string type on the objects in ETABS
            // as long as tags are unique it should be ok to store tags here - it looks like all elements have get/set for GUID.
            // 'name' and 'lable' (non editable from UI and API) 

            string name = "";
            if (!bhNode.CustomData.ContainsKey(AdapterId))
                bhNode.CustomData.Add(AdapterId, "thisWillBeChanged");
            else
                name = bhNode.CustomData[AdapterId].ToString();
            
            model.PointObj.AddCartesian(bhNode.Position.X, bhNode.Position.Y, bhNode.Position.Z, ref name);

            //TODO: update the BHOM Node with the id acctually assigned in ETABS, or don't - not sure which would best align with behaviour of other adapters
            //bhNode.CustomData[AdapterId] = name;

            //if (bhNode.CustomData.ContainsKey(AdapterId))
            //    bhNode.CustomData.Add(AdapterId, name);

            if(bhNode.Constraint != null)
            {
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
            }

            ////it is likely that the tags used needs to be unique to work in etabs - add object id or guid to the tag hashset?
            //model.PointObj.SetGUID(name, bhNode.TaggedName());

            return name;
        }

        public static void ToETABS(this Bar bhBar, cSapModel model)
        {
            string name = bhBar.CustomData[AdapterId].ToString();
            //get all node ids to chack if bar is using a node that has already been pushed... if this is not handled elsewhere in the BHoMAdapter already

            int ptCount = 0;
            string[] ids = null;
            double[] nX = null;
            double[] nY = null;
            double[] nZ = null;
            
            model.PointObj.GetAllPoints(ref ptCount, ref ids, ref nX, ref nY, ref nZ);
            //the above should be stored in a 'modelInfo' field like in the RFEM adapter

            string ptA;
            string ptB;

            bhBar.StartNode.ToETABS(model);
            bhBar.EndNode.ToETABS(model);

            bool startExists = ids.Contains(bhBar.StartNode.CustomData[AdapterId].ToString());
            bool endExists = ids.Contains(bhBar.EndNode.CustomData[AdapterId].ToString());
            bool bothExists = endExists == true && startExists == true;

            if (bothExists)
            {
                ptA = bhBar.StartNode.CustomData[AdapterId].ToString();
                ptB = bhBar.EndNode.CustomData[AdapterId].ToString();
            }
            else if (startExists)
            {
                ptA = bhBar.StartNode.CustomData[AdapterId].ToString();
                ptB = bhBar.EndNode.ToETABS(model);
            }
            else if (endExists)
            {
                ptA = bhBar.StartNode.ToETABS(model);
                ptB = bhBar.EndNode.CustomData[AdapterId].ToString();
            }
            else
            {
                ptA = bhBar.StartNode.ToETABS(model);
                ptB = bhBar.EndNode.ToETABS(model);
            }

            model.FrameObj.AddByPoint(ptA, ptB, ref name);

            //model.FrameObj.SetGUID(name, bhNode.TaggedName());// see comment on node convert
            //model.FrameObj.SetSection();
            //model.FrameObj.SetReleases();
            //model.FrameObj.SetGroupAssign();
        }
    }
}