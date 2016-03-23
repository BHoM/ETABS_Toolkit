using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ETABS2015;
using BHoM;

namespace ETABSToolkit
{
    public class ETABSApp
    {
        static cOAPI _ETABSObject;
        static cHelper _myHelper;
        static cSapModel _sapModel;
        string _pathToETABS;

        /// <summary>
        /// Default constructor of the ETABS API
        /// </summary>
        /// <param name="filePath">location of ETABS.exe if it is not in default location</param>
        public ETABSApp(string filePath = null)
        {
            _myHelper = new Helper();
            if (filePath == null)
                _pathToETABS = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES"), "Computers and Structures", "ETABS 2015", "ETABS.exe");
            else
                _pathToETABS = filePath;
        }

        public void StartNewInstance()
        {
            _ETABSObject = _myHelper.CreateObject(_pathToETABS);
            _sapModel.InitializeNewModel(eUnits.kN_mm_C);
        }

        public void GetRunningInstance()
        {
            //check if there is a running instance - if not it might cause app hang
            _ETABSObject = _myHelper.GetObject("CSI.ETABS.API.ETABSObject");
            _sapModel = _ETABSObject.SapModel;
            _sapModel.SetPresentUnits(eUnits.kN_m_C);//<----------------- this should be a temporary setting for MotF. TODO
        }

        public cSapModel GetSapModel()
        {
            return _sapModel;
        }

        public void SaveFile(string filePath = null)
        {
            string path;
            if (filePath == null)
                path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            else
                path = filePath;

            _sapModel.File.Save(path);
        }

        public void CloseAndClean(bool save = false)
        {
            _ETABSObject.ApplicationExit(save);
            _sapModel = null;
            _myHelper = null;
            _ETABSObject = null;
        }

        public void CleanDontClose()
        {
            _sapModel = null;
            _myHelper = null;
            _ETABSObject = null;
        }

        public Dictionary<int, double[]> ReadPointDataFromETABS(bool allPoints)
        {
            List<int> test = new List<int>();

            int numberName = 0; //not certain of the function of this
            int pointCount = _sapModel.PointObj.Count();

            string[] pointIDArr = new string[pointCount - 1];

            _sapModel.PointObj.GetNameList(ref numberName, ref pointIDArr);

            //point coordinates
            List<double[]> ptCoords = new List<double[]>();
            Dictionary<int, double[]> pointID = new Dictionary<int, double[]>();
            double x = 0;
            double y = 0;
            double z = 0;
            bool selected = false;

            for (int i = 0; i < pointIDArr.Length; i++)
            {
                _sapModel.PointObj.GetSelected(pointIDArr[i], ref selected);

                if (allPoints | selected)
                {
                    _sapModel.PointObj.GetCoordCartesian(pointIDArr[i], ref x, ref y, ref z);
                    pointID.Add(Convert.ToInt32(pointIDArr[i]), new double[3] { x, y, z });
                    test.Add(Convert.ToInt32(pointIDArr[i]));
                }
            }

            return pointID;
        }


        public void SetPointLoad(Dictionary<int, double[]> ptLoads, string loadPattern)
        {
            foreach (KeyValuePair<int, double[]> kvp in ptLoads)
            {
                double[] tmp = kvp.Value;
                _sapModel.PointObj.SetLoadForce(kvp.Key.ToString(), loadPattern, ref tmp, true);
            }
        }

        public void SetPointLoad(int ID, double[] force, string loadpattern)
        {
            _sapModel.PointObj.SetLoadForce(ID.ToString(), loadpattern, ref force, true);
        }
    }
}
