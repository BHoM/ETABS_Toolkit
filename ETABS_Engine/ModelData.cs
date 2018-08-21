using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Properties;
using BH.Engine.Structure;
using BH.oM.Common.Materials;
using ETABS2016;

namespace BH.Engine.ETABS
{
    public class ModelData
    {
        public Dictionary<string, ISectionProperty> sectionDict;
        public Dictionary<string, Material> materialDict;
        public cSapModel model;

        public ModelData(cSapModel data)
        {
            sectionDict = new Dictionary<string, ISectionProperty>();
            materialDict = new Dictionary<string, Material>();
            model = data;
        }
    }
}
