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
            // !!! note: BHoM is setup to handle master-slave as 1toMany relation, but ETABS needs a 1to1 relation. 
            //           The code below assumes that the input has been supplied as two lists of equal length, one for master ad one for slave.
            //           the problem is that the BHoM object representing the rigidLink is a rigidLink from ALL master nodes to ALL slave nodes !!! Bad-Bad-Not-Good !!!

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

            for (int i = 0; i < rigidLinks.Count; i++)
            {
                RigidLink link = rigidLinks[i];

                LinkConstraint constraint = link.Constraint;
                //master
                XJ = link.MasterNode.CartesianCoordinates[0]*1000;//multiply by 1000 to compensate for Etabs strangeness: yes, one end is divided by 1000 the other end is not!
                YJ = link.MasterNode.CartesianCoordinates[1]*1000;
                ZJ = link.MasterNode.CartesianCoordinates[2]*1000;

                //slave
                XI = link.SlaveNodes[i].CartesianCoordinates[0];
                YI = link.SlaveNodes[i].CartesianCoordinates[1];
                ZI = link.SlaveNodes[i].CartesianCoordinates[2];

                int r = SapModel.LinkObj.AddByCoord(XI, YI, ZI, XJ, YJ, ZJ, ref name);
                if (r != 0) { success = false; }

                EtabsUtils.SetDefaultKeyData(link.CustomData, name);//this is not the way if it is to support multiple slave nodes
                ids.Add(name);
            }

            return success;
        }

    }
}
