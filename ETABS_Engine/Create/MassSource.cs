using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Loads;


namespace BH.Engine.Etabs.Structure
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static MassSource MassSource(bool elementSelfWeight, bool additionalMass, List<Loadcase> loadCases = null, List<double> caseFactors = null)
        {
            List<Tuple<Loadcase, double>> factoredCases = new List<Tuple<Loadcase, double>>();

            if (loadCases != null)
            {
                if (caseFactors == null)
                {
                    Engine.Reflection.Compute.RecordError("If cases are provided, please provide factors as well");
                    return null;
                }

                if (loadCases.Count != caseFactors.Count)
                {
                    Engine.Reflection.Compute.RecordError("Please provide the same number of cases and case factors");
                    return null;
                }

                for (int i = 0; i < loadCases.Count; i++)
                {
                    factoredCases.Add(new Tuple<Loadcase, double>(loadCases[i], caseFactors[i]));
                }
            }
            else if (caseFactors != null)
            {
                Engine.Reflection.Compute.RecordError("If factors are provided, please provide cases as well");
                return null;
            }

            return new MassSource { ElementSelfMass = elementSelfWeight, AdditionalMass = additionalMass, FactoredAdditionalCases = factoredCases };
        }

        /***************************************************/
    }
}
