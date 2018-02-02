using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        private Dictionary<Type, int> idDictionary = new Dictionary<Type, int>();

        protected override object NextId(Type objectType, bool refresh)
        {
            int index;
            if(!refresh && idDictionary.TryGetValue(objectType, out index))
            {
                index++;
                idDictionary[objectType] = index;
            }
            else
            {
                index = GetLastIdOfType(objectType) + 1;
                idDictionary[objectType] = index;
            }

            return index;
        }

        private int GetLastIdOfType(Type objectType)
        {
            int lastId;
            string typeString = objectType.ToString();

            switch (typeString)
            {
                case "Node":
                    lastId = model.PointObj.Count();// this is not sufficient !!!!
                    break;
                case "Bar":
                    lastId = model.FrameObj.Count();
                    break;
                case "Material":
                    lastId = 0;
                    break;
                case "SectionProperty":
                    lastId = 0;
                    break;

                default:
                    lastId = 0;//<---- log error
                    break;
            }

            return lastId;

        }
    }
}
