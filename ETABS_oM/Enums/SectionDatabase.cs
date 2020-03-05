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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.oM.Adapters.ETABS
{
    public enum SectionDatabase
    {
        AISC14 = 1,
        AISC14M = 2,
        AISC15 = 3,
        AISC15M = 4,
        ArcelorMittal_British = 5,
        ArcelorMittal_BritishHISTAR = 6,
        ArcelorMittal_Europe = 7,
        ArcelorMittal_EuropeHISTAR = 8,
        ArcelorMittal_Japan = 9,
        ArcelorMittal_Russia = 10,
        ArcelorMittal_US_ASTM_A913 = 11,
        ArcelorMittal_US_ASTM_A913M = 12,
        ArcelorMittal_US_ASTM_A992 = 13,
        ArcelorMittal_US_ASTM_A992M = 14,
        Australia_NewZealand = 15,
        BSShapes2006 = 16,
        ChineseGB08 = 17,
        CISC9 = 18,
        CISC10 = 19,
        CoreBraceBRB_2016 = 20,
        Euro = 21,
        Indian = 22,
        JIS_G_3192_2014 = 23,
        Nordic = 24,
        Russian = 25,
        SJIJoists = 26,
        StarSeismicBRB = 27
    }
}

