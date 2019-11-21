/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2019, the respective contributors. All rights reserved.
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
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using BH.oM.Reflection.Attributes;
using BH.oM.Base;
using BH.oM.Structure.Requests;
using BH.oM.Data.Requests;
using BH.oM.Structure.Results;

namespace BH.Engine.ETABS
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("")]
        [Input("", "")]
        [Output("", "")]
        public static IResultRequest ToResultRequest(Type type, IList ids, IList cases, int divisions)
        {
            IResultRequest request = null;

            if (typeof(BarResult).IsAssignableFrom(type))
            {
                BarResultType resType = BarResultType.BarForce;

                if (type == typeof(BarForce))
                    resType = BarResultType.BarForce;
                else if (type == typeof(BarDeformation))
                    resType = BarResultType.BarDeformation;
                else if (type == typeof(BarStress))
                    resType = BarResultType.BarStress;
                else if (type == typeof(BarStrain))
                    resType = BarResultType.BarStrain;

                request = new BarResultRequest { Divisions = divisions, DivisionType = DivisionType.EvenlyDistributed, ResultType = resType };
            }
            else if (typeof(MeshResult).IsAssignableFrom(type) || typeof(MeshElementResult).IsAssignableFrom(type))
            {
                MeshResultType resType = MeshResultType.Forces;

                if (type == typeof(MeshForce))
                    resType = MeshResultType.Forces;
                else if (type == typeof(MeshStress))
                    resType = MeshResultType.Stresses;
                else if (type == typeof(MeshVonMises))
                    resType = MeshResultType.VonMises;
                else if (type == typeof(MeshDisplacement))
                    resType = MeshResultType.Displacements;

                request = new MeshResultRequest { ResultType = resType };

            }
            else if (typeof(StructuralGlobalResult).IsAssignableFrom(type))
            {
                GlobalResultType resType = GlobalResultType.Reactions;

                if (type == typeof(GlobalReactions))
                    resType = GlobalResultType.Reactions;
                else if (type == typeof(ModalDynamics))
                    resType = GlobalResultType.ModalDynamics;

                request = new GlobalResultRequest { ResultType = resType };
            }
            else if (typeof(NodeResult).IsAssignableFrom(type))
            {
                NodeResultType resType = NodeResultType.NodeReaction;

                if (type == typeof(NodeReaction))
                    resType = NodeResultType.NodeReaction;
                else if (type == typeof(NodeDisplacement))
                    resType = NodeResultType.NodeDisplacement;
                else if (type == typeof(NodeAcceleration))
                    resType = NodeResultType.NodeAcceleration;
                else if (type == typeof(NodeVelocity))
                    resType = NodeResultType.NodeVelocity;

                request = new NodeResultRequest { ResultType = resType };
            }
            else
            {
                return null;
            }


            if (ids != null)
                request.ObjectIds = ids.Cast<object>().ToList();

            if (cases != null)
                request.Cases = cases.Cast<object>().ToList();

            return request;

        }

        /***************************************************/

        

        /***************************************************/
    }
}
