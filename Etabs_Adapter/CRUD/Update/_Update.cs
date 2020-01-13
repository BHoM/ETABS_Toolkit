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

using System.Collections.Generic;
using System.Linq;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Loads;
using BH.Engine.Structure;
using BH.Engine.Geometry;
using BH.oM.Structure.MaterialFragments;
using BH.Engine.ETABS;
using BH.oM.Adapters.ETABS.Elements;
using BH.oM.Adapter;

namespace BH.Adapter.ETABS
{
#if Debug17 || Release17
    public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABS2016Adapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Adapter override methods                  ****/
        /***************************************************/

        protected override bool IUpdate<T>(IEnumerable<T> objects, ActionConfig actionConfig = null)
        {
            if (typeof(T) == typeof(Node))
            {
                return UpdateObjects(objects as IEnumerable<Node>);
            }
            if (typeof(T) == typeof(Panel))
            {
                return UpdateObjects(objects as IEnumerable<Panel>);
            }
            else
                return base.IUpdate<T>(objects, actionConfig);
        }

        /***************************************************/

        private bool UpdateObjects(IEnumerable<Node> nodes)
        {
            bool sucess = true;
            foreach (Node bhNode in nodes)
            {
                if (bhNode.Support != null)
                {
                    string name = bhNode.CustomData[AdapterIdName].ToString();

                    bool[] restraint = new bool[6];
                    restraint[0] = bhNode.Support.TranslationX == DOFType.Fixed;
                    restraint[1] = bhNode.Support.TranslationY == DOFType.Fixed;
                    restraint[2] = bhNode.Support.TranslationZ == DOFType.Fixed;
                    restraint[3] = bhNode.Support.RotationX == DOFType.Fixed;
                    restraint[4] = bhNode.Support.RotationY == DOFType.Fixed;
                    restraint[5] = bhNode.Support.RotationZ == DOFType.Fixed;

                    double[] spring = new double[6];
                    spring[0] = bhNode.Support.TranslationalStiffnessX;
                    spring[1] = bhNode.Support.TranslationalStiffnessY;
                    spring[2] = bhNode.Support.TranslationalStiffnessZ;
                    spring[3] = bhNode.Support.RotationalStiffnessX;
                    spring[4] = bhNode.Support.RotationalStiffnessY;
                    spring[5] = bhNode.Support.RotationalStiffnessZ;

                    sucess &= m_model.PointObj.SetRestraint(name, ref restraint) == 0;
                    sucess &= m_model.PointObj.SetSpring(name, ref spring) == 0;
                }
            }

            return sucess;
        }

        private bool UpdateObjects(IEnumerable<Panel> bhPanels)
        {
            bool sucess = true;

            foreach (Panel bhPanel in bhPanels)
            {
                Pier pier = bhPanel.Pier();
                Spandrel spandrel = bhPanel.Spandrel();
                List<string> pl = new List<string>();
                string name = bhPanel.CustomData[AdapterIdName].ToString();

                if (pier != null)
                {
                    int ret = m_model.PierLabel.SetPier(pier.Name);
                    ret = m_model.AreaObj.SetPier(name, pier.Name);
                }
                if (spandrel != null)
                {
                    int ret = m_model.SpandrelLabel.SetSpandrel(spandrel.Name, false);
                    ret = m_model.AreaObj.SetSpandrel(name, spandrel.Name);
                }
            }
            return sucess;
        }


        /***************************************************/
    }
}
