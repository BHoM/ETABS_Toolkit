/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2024, the respective contributors. All rights reserved.
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
using BH.Engine.Adapter;
using BH.oM.Adapters.ETABS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Structure.Results;
using BH.oM.Analytical.Results;
using BH.oM.Structure.Requests;
using BH.oM.Adapter;
using BH.oM.Structure.Loads;

namespace BH.Adapter.ETABS
{
#if Debug16 || Release16
    public partial class ETABS2016Adapter : BHoMAdapter
#elif Debug17 || Release17
   public partial class ETABS17Adapter : BHoMAdapter
#else
    public partial class ETABSAdapter : BHoMAdapter
#endif
    {
        /***************************************************/
        /**** Public method - Read override             ****/
        /***************************************************/

        public IEnumerable<IResult> ReadResults(GlobalResultRequest request, ActionConfig actionConfig = null)
        {
            CheckAndSetUpCases(request);

            switch (request.ResultType)
            {
                case GlobalResultType.Reactions:
                    return GetGlobalReactions();
                case GlobalResultType.ModalDynamics:
                    return GetModalParticipationMassRatios();
                default:
                    Engine.Base.Compute.RecordError("Result extraction of type " + request.ResultType + " is not yet supported");
                    return new List<IResult>();
            }
        }

        /***************************************************/
        /**** Private method - Extraction methods       ****/
        /***************************************************/

        private List<GlobalReactions> GetGlobalReactions()
        {
            List<GlobalReactions> globalReactions = new List<GlobalReactions>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] stepType = null; double[] stepNum = null;
            double[] fx = null; double[] fy = null; double[] fz = null;
            double[] mx = null; double[] my = null; double[] mz = null;
            double gx = 0; double gy = 0; double gz = 0;

            m_model.Results.BaseReact(ref resultCount, ref loadcaseNames, ref stepType, ref stepNum, ref fx, ref fy, ref fz, ref mx, ref my, ref mz, ref gx, ref gy, ref gz);

            for (int i = 0; i < resultCount; i++)
            {
                int mode;
                double timeStep;
                GetStepAndMode(stepType[i], stepNum[i], out timeStep, out mode);

                GlobalReactions g = new GlobalReactions("", loadcaseNames[i], mode, timeStep, fx[i], fy[i], fz[i], mx[i], my[i], mz[i]);

                globalReactions.Add(g);
            }

            return globalReactions;
        }

        /***************************************************/

        private List<ModalDynamics> GetModalParticipationMassRatios()
        {
            //TODO: This method is to be fixed before exposed. Currently does not give back ratios when it should, but full values
            //All arguments to be fully checked through and refactored before exposed
            List<ModalDynamics> partRatios = new List<ModalDynamics>();

            int resultCount = 0;
            string[] loadcaseNames = null;
            string[] stepType = null; double[] stepNum = null;
            double[] period = null;
            double[] massRatioX = null; double[] massRatioY = null; double[] massRatioZ = null;
            double[] inertiaRatioX = null; double[]inertiaRatioY = null; double[]inertiaRatioZ = null;
            double[] sumMassRatioX = null; double[] sumMassRatioY = null; double[] sumMassRatioZ = null;
            double[] sumInertiaRatioX = null; double[] sumInertiaRatioY = null; double[] sumInertiaRatioZ = null;
            double[] ux = null; double[] uy = null; double[] uz = null;
            double[] rx = null; double[] ry = null; double[] rz = null;
            double[] modalMass = null;
            double[] modalStiff = null;

            int res=m_model.Results.ModalParticipatingMassRatios(ref resultCount, ref loadcaseNames, ref stepType, ref  stepNum, ref  period, 
                                                                 ref massRatioX, ref massRatioY, ref massRatioZ, ref sumMassRatioX, ref sumMassRatioY, ref sumMassRatioZ, 
                                                                 ref inertiaRatioX, ref inertiaRatioY, ref inertiaRatioZ, 
                                                                 ref sumInertiaRatioX, ref sumInertiaRatioY, ref sumInertiaRatioZ);

            res = m_model.Results.ModalParticipationFactors(ref resultCount, ref loadcaseNames, ref stepType, ref stepNum,
                ref period, ref ux, ref uy, ref uz, ref rx, ref ry, ref rz, ref modalMass, ref modalStiff);


            if (res != 0) Engine.Base.Compute.RecordError("Could not extract Modal information.");

            // Although API documentation says that StepNumber should correspond to the Mode Number, testing shows that StepNumber is always 0.
            string previousModalCase = "";
            int modeNumber = 1; //makes up for stepnumber always = 0
            for (int i = 0; i < resultCount; i++)
            {
                if (loadcaseNames[i] != previousModalCase)
                    modeNumber = 1;

                ModalDynamics mod = new ModalDynamics("", loadcaseNames[i], modeNumber, 0, 1 / period[i], modalMass[i], modalStiff[i], 0, 
                                                        massRatioX[i], massRatioY[i], massRatioZ[i], inertiaRatioX[i], inertiaRatioY[i], inertiaRatioZ[i]);
                modeNumber += 1;
                previousModalCase = loadcaseNames[i];

                partRatios.Add(mod);
            }

            return partRatios;
        }

        /***************************************************/

    }
}






