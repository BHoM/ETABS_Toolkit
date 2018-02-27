using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.Engine.ETABS;
using BH.oM;
using BH.oM.Structural;
using BH.oM.Structural.Elements;
using BH.oM.Structural.Properties;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        protected override bool Create<T>(IEnumerable<T> objects, bool replaceAll = false)
        {
            bool success = true;

            if (typeof(BH.oM.Base.IBHoMObject).IsAssignableFrom(typeof(T)))
            {
                foreach (T obj in objects)
                {
                    success = CreateObject(obj as dynamic, modelData);
                    //if (!success)
                    //    break;
                    //((BH.oM.Base.IBHoMObject)obj).ToETABS(modelData);
                }
            }
            else
            {
                success = false;
            }

            modelData.model.View.RefreshView();
            return success;
        }

        private bool CreateObject(Node bhNode, ModelData modelData)
        {
            bool success = true;
            int retA = 0;
            int retB = 0;
            int retC = 0;

            string name = "";
            string bhId = bhNode.CustomData[AdapterId].ToString();
            name = bhId;

            retA = modelData.model.PointObj.AddCartesian(bhNode.Position.X, bhNode.Position.Y, bhNode.Position.Z, ref name);
            if (name != bhId)
                success = false; //this is not necessary if you can guarantee that it is impossible that this bhId does not match any existing name in ETABS !!!

            if (bhNode.Constraint != null)
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

                retB = modelData.model.PointObj.SetRestraint(name, ref restraint);
                retC = modelData.model.PointObj.SetSpring(name, ref spring);
            }

            if (retA != 0 || retB != 0 || retC != 0)
                success = false;

            return success;
        }

        private bool CreateObject(Bar bhBar, ModelData modelData)
        {
            bool success = true;
            int retA = 0;
            int retB = 0;
            int retC = 0;

            string name = "";
            string bhId = bhBar.CustomData[AdapterId].ToString();
            name = bhId;

            retA = modelData.model.FrameObj.AddByPoint(bhBar.StartNode.CustomData[AdapterId].ToString(), bhBar.EndNode.CustomData[AdapterId].ToString(), ref name);
            if (bhId != name)
                success = false;

            //model.FrameObj.SetGUID(name, bhNode.TaggedName());// see comment on node convert
            retB = modelData.model.FrameObj.SetSection(name, bhBar.SectionProperty.Name);
            //model.FrameObj.SetReleases();
            //model.FrameObj.SetGroupAssign();
            if (retA != 0 || retB != 0 || retC != 0)
                success = false;

            return success;
        }

        private bool CreateObject(ISectionProperty bhSection, ModelData modelData)
        {
            bool success = true;

            BH.Engine.ETABS.Convert.SetSectionProperty(modelData, bhSection);//TODO: this is only halfway done - should be moved away from engine to adapter as much as possible

            return success;
        }

        //private bool CreateObject(PanelPlanar bhPanel, ModelData modelData)
        //{
        //    bool success = true;

        //    bhPanel.ExternalEdges = null;
        //    int segmentCount

        //    modelData.model.AreaObj.AddByCoord()

        //    return success;
        //}
    }
}
