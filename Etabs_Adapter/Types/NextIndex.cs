/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Adapter.ETABS
{
    public partial class ETABSAdapter
    {
        private Dictionary<Type, string> idDictionary = new Dictionary<Type, string>();

        protected override object NextId(Type objectType, bool refresh)
        {
            //int index;
            //string id;
            //if(!refresh && idDictionary.TryGetValue(objectType, out id))
            //{
            //    if (int.TryParse(id, out index))
            //        id = (index + 1).ToString();
            //    else
            //        id = GetNextIdOfType(objectType);
            //    idDictionary[objectType] = id;
            //}
            //else
            //{
            //    id = GetNextIdOfType(objectType);
            //    idDictionary[objectType] = id;
            //}

            return -1;
        }

        private string GetNextIdOfType(Type objectType)
        {
            string lastId;
            int lastNum;
            string typeString = objectType.Name;
            int nameCount = 0;
            string[] names = { };

            switch (typeString)
            {
                case "Node":
                    m_model.PointObj.GetNameList(ref nameCount, ref names);
                    lastNum = nameCount == 0 ? 1 : Array.ConvertAll(names, int.Parse).Max() + 1;
                    lastId = lastNum.ToString();
                    break;
                case "Bar":
                    m_model.FrameObj.GetNameList(ref nameCount, ref names);
                    lastNum = nameCount == 0 ? 1 : Array.ConvertAll(names, int.Parse).Max() + 1;
                    lastId = lastNum.ToString();
                    break;
                case "Panel":
                    m_model.AreaObj.GetNameList(ref nameCount, ref names);
                    lastNum = nameCount == 0 ? 1 : Array.ConvertAll(names, int.Parse).Max() + 1;
                    lastId = lastNum.ToString();
                    break;
                case "Material":
                    m_model.PropMaterial.GetNameList(ref nameCount, ref names);
                    lastId = typeString + "-" + (nameCount + 1).ToString();
                    break;
                case "SectionProperty":
                    m_model.PropFrame.GetNameList(ref nameCount, ref names);
                    lastId = typeString + "-" + (nameCount + 1).ToString();
                    break;
                case "Property2D":
                    m_model.PropArea.GetNameList(ref nameCount, ref names);
                    lastId = typeString + "-" + (nameCount + 1).ToString();
                    break;
                case "Loadcase":
                    m_model.LoadPatterns.GetNameList(ref nameCount, ref names);
                    lastId = typeString + "-" + (nameCount + 1).ToString();
                    break;
                case "LoadCombination":
                    m_model.AreaObj.GetNameList(ref nameCount, ref names);
                    lastId = typeString + "-" + (nameCount + 1).ToString();
                    break;
                case "RigidLink":
                    m_model.LinkObj.GetNameList(ref nameCount, ref names);
                    lastId = typeString + "-" + (nameCount + 1).ToString();
                    break;
                default:
                    lastId = "0";
                    ErrorLog.Add("Could not get count of type: " + typeString);
                    break;
            }

            return lastId;

        }

    }
}
