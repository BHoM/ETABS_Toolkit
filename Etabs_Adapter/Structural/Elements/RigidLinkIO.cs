using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM.Base;
using BHoM.Geometry;
using BHoM.Materials;
using BHoM.Structural.Elements;
using BHoM.Structural.Interface;
using BHoM.Structural.Properties;
using Etabs_Adapter.Base;
using Etabs_Adapter.Structural.Properties;
using ETABS2016;


namespace Etabs_Adapter.Structural.Elements
{
    public class RigidLinkIO
    {
        public static bool SetRidgidLinks(cOAPI Etabs, List<RigidLink> rigidLinks, out List<string> ids)
        {
            cSapModel SapModel = Etabs.SapModel;
            ids = new List<string>();
            bool success = true;

            double XI;
            double YI;
            double ZI;
            double XJ;
            double YJ;
            double ZJ;
            string name = "";
            bool IsSingleJoint = false;
            string PropName = "Default";
            string UserName = "";
            string CSys = "Global";

            foreach(RigidLink link in rigidLinks)
            {
                LinkConstraint constraint = link.Constraint;

                XJ = link.MasterNode.CartesianCoordinates[0];
                YJ = link.MasterNode.CartesianCoordinates[1];
                ZJ = link.MasterNode.CartesianCoordinates[2];

                foreach(Node slave in link.SlaveNodes)
                {
                    //do something clever to handle the fact that etabs links are 1to1 and not 1toMany relation !
                    XI = slave.CartesianCoordinates[0];
                    YI = slave.CartesianCoordinates[1];
                    ZI = slave.CartesianCoordinates[2];

                    int r = SapModel.LinkObj.AddByCoord(XI, YI, ZI, XJ, YJ, ZJ, ref name);
                    if (r != 0) { success = false; }

                    EtabsUtils.SetDefaultKeyData(link.CustomData, name);//this is not the way if it is to support multiple slave nodes
                    ids.Add(name);
                }
            }

            return success;
        }

    }
}
