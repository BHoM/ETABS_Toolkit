using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using System.Runtime.Remoting.Lifetime;


namespace ETABSToolkit
{
   public  class OpenETABS
    {
        public static object Open(string FilePath = "", Boolean Activate = false)
        {
          string pathtoetabs = @"C:\Program Files\Computers and Structures\ETABS 2013\ETABS.exe";
          System.Reflection.Assembly ETABSAssembly = System.Reflection.Assembly.LoadFrom(pathtoetabs);
          object newInstance = ETABSAssembly.CreateInstance("CSI.ETABS.API.ETABSObject");
          ETABS2015.cOAPI ETABSObject = null;
            
          int attempt = 0;
          int ret;
          if (Activate)
          {
              while (attempt++ <= 10)
              {
                  try
                  {
                      ETABSObject = (ETABS2015.cOAPI)newInstance;
                      break;
                  }
                  catch (Exception ex)
                  {
                      if (attempt == 10)
                      {
                          return ETABSObject;
                      }
                  }
              }
          }
                if (ETABSObject != null)
                {
                   ret = ETABSObject.ApplicationStart();
               //     ETABSObject.SapModel.InitializeNewModel();
             //  ETABSObject.SapModel.File.NewBlank(); NOTE THIS GIVES AN ERROR SO USING DUMMY FILE INSTEAD
                   ETABSObject.SapModel.File.OpenFile("C:\\Users\\epiermar\\Desktop\\etabs test\\test.EDB");
                   ETABSObject.SapModel.SetPresentUnits(ETABS2015.eUnits.kgf_m_C);
                }
                return ETABSObject;
        }
    }
}
