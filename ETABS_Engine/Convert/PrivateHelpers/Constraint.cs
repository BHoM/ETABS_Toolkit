using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structural.Properties;

namespace BH.Engine.ETABS
{
    public static partial class Convert
    {
        public static Constraint6DOF GetConstraint6DOF(bool[] restraint, double[] springs)
        {
            Constraint6DOF bhConstraint = new Constraint6DOF();
            bhConstraint.TranslationX = restraint[0] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.TranslationY = restraint[1] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.TranslationZ = restraint[2] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.RotationX = restraint[3] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.RotationY = restraint[4] == true ? DOFType.Fixed : DOFType.Free;
            bhConstraint.RotationZ = restraint[5] == true ? DOFType.Fixed : DOFType.Free;

            bhConstraint.TranslationalStiffnessX = springs[0];
            bhConstraint.TranslationalStiffnessY = springs[1];
            bhConstraint.TranslationalStiffnessZ = springs[2];
            bhConstraint.RotationalStiffnessX = springs[3];
            bhConstraint.RotationalStiffnessY = springs[4];
            bhConstraint.RotationalStiffnessZ = springs[5];

            return bhConstraint;
        }


    }
}
