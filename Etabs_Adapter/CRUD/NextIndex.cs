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
            string typeString = objectType.Name;
            int nameCount =0;
            string[] names = { };

            switch (typeString)
            {
                case "Node":
                    model.PointObj.GetNameList(ref nameCount, ref names);
                    lastId = nameCount == 0 ? 0 : Array.ConvertAll(names, int.Parse).Max();
                    break;
                case "Bar":
                    model.FrameObj.GetNameList(ref nameCount, ref names);
                    lastId = nameCount == 0 ? 0 : Array.ConvertAll(names, int.Parse).Max();
                    break;
                case "Material":
                    model.PropMaterial.GetNameList(ref nameCount, ref names);
                    lastId = nameCount;//'name' is not a int-convertible string
                    break;
                case "SectionProperty":
                    model.PropFrame.GetNameList(ref nameCount, ref names);
                    lastId = nameCount == 0 ? 0 : Array.ConvertAll(names, int.Parse).Max();
                    break;
                case "Property2D":
                    model.PropArea.GetNameList(ref nameCount, ref names);
                    lastId = nameCount;//'name' is not a int-convertible string
                    break;
                case "PanelPlanar":
                    model.AreaObj.GetNameList(ref nameCount, ref names);
                    lastId = nameCount;//'name' is not a int-convertible string
                    break;

                default:
                    lastId = 0;
                    ErrorLog.Add("Could not get count of type: " + typeString);
                    break;
            }

            return lastId;

        }
    }
}
