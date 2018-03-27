using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structural.Results;
using BH.oM.Common;
using ETABS2016;

namespace BH.Adapter.ETABS
{
    public static partial class Helper
    {
        #region Node Results

        public static List<IResult> GetNodeAcceleration(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }

        public static List<IResult> GetNodeDisplacement(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }

        public static List<IResult> GetNodeReaction(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }

        public static List<IResult> GetNodeVelocity(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }
        #endregion

        #region bar Results

        public static List<IResult> GetBarDeformation(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }

        public static List<IResult> GetBarForce(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }

        public static List<IResult> GetBarStrain(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }

        public static List<IResult> GetBarStress(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }


        #endregion

        #region Panel Results

        public static List<IResult> GetPanelForce(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }

        public static List<IResult> GetPanelStress(cSapModel model, IList ids = null, IList cases = null, int divisions = 5)
        {

            return new List<IResult>();
        }


        #endregion
    }
}
