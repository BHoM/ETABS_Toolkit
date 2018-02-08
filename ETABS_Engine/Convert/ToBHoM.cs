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

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        /// <summary>
        /// ids = null returns all nodes in model
        /// </summary>
        public static List<Node> ToBHoM(this cPointObj pointObj, List<string> ids)
        {
            List<Node> bhNodes = new List<Node>();
            int nameCount = 0;
            string[] nameArr = { };

            if (ids == null)
            {
                pointObj.GetNameList(ref nameCount, ref nameArr);
                ids = nameArr.ToList();
            }

            double x, y, z;
            x = y = z = 0;
            bool[] restraint = new bool[6];
            double[] spring = new double[6];


            foreach (string id in ids)
            {
                Node bhNode = new Node();
                pointObj.GetCoordCartesian(id, ref x, ref y, ref z);
                bhNode.Position = new oM.Geometry.Point() { X = x, Y = y, Z = z };
                bhNode.CustomData.Add(AdapterId, id);

                restraint[0] = bhNode.Constraint.TranslationX == DOFType.Fixed;
                restraint[1] = bhNode.Constraint.TranslationY == DOFType.Fixed;
                restraint[2] = bhNode.Constraint.TranslationZ == DOFType.Fixed;
                restraint[3] = bhNode.Constraint.RotationX == DOFType.Fixed;
                restraint[4] = bhNode.Constraint.RotationY == DOFType.Fixed;
                restraint[5] = bhNode.Constraint.RotationZ == DOFType.Fixed;
                pointObj.GetRestraint(id, ref restraint);


                spring[0] = bhNode.Constraint.TranslationalStiffnessX;
                spring[1] = bhNode.Constraint.TranslationalStiffnessY;
                spring[2] = bhNode.Constraint.TranslationalStiffnessZ;
                spring[3] = bhNode.Constraint.RotationalStiffnessX;
                spring[4] = bhNode.Constraint.RotationalStiffnessY;
                spring[5] = bhNode.Constraint.RotationalStiffnessZ;
                pointObj.SetSpring(id, ref spring);



                bhNodes.Add(bhNode);
            }
            return bhNodes;
        }


        public static Node ToBHoM(this cPointObj point, string id)
        {
            // use this inside the above List<Node> overload method ?!!! 


            string name = "noPoint"; //wtf? there looks to be no way fo getting the coordinates withouot the name/id of the node!!!
            double x = 0;
            double y = 0;
            double z = 0;
            point.GetCoordCartesian(name, ref x, ref y, ref z);

            Node bhNode = new Node();
            bhNode.Name = name;
            bhNode.Position = new oM.Geometry.Point { X = x, Y = y, Z = z };

            //add conversion for other properties !!!
            
            return bhNode;
        }
         

        //public static Bar ToBHoM(this cFrameObj bar)
        //{


        //}
    }
}