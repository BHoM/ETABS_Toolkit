/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
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


using BH.oM.Base;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.Adapters.ETABS
{
    public class ETABSId : IAdapterId, IPersistentAdapterId
    {
        /***************************************************/
        /**** Public Properties                         ****/
        /***************************************************/

        [Description("Id or multi-ids of the element as assigned in ETABS.")]
        public virtual object Id { get; set; }

        [Description("Label of the element in Etabs. Only populated for Element-type objects.")]
        public virtual object Label { get; set; } = "";

        [Description("Name of the story of the element in Etabs. Only populated for Element-type objects.")]
        public virtual object Story { get; set; } = "";

        [Description("Persistent GUID assigned by ETABS upon element creation. This does not vary when the element is modified. Only populated for Element-type objects.")]
        public virtual object PersistentId { get; set; }

        /***************************************************/
    }
}

