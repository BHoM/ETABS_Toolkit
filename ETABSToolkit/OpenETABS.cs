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
        /// <summary>
        /// Opens an instance of ETABS  engine
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="Activate"></param>
        public static void Open(string FilePath = "", Boolean Activate = false)
        {
            string pathtoetabs = @"C:\Program Files\Computers and Structures\ETABS 2013\ETABS.exe";
            System.Reflection.Assembly ETABSAssembly = System.Reflection.Assembly.LoadFrom(pathtoetabs);
            object newInstance = ETABSAssembly.CreateInstance("CSI.ETABS.API.ETABSObject");
             ETABS2013.cOAPI ETABSObject = null;
            
                int attempt = 0;
          int ret;
                while (attempt++ <= 10)
                {
                    try
                    {
                        ETABSObject = (ETABS2013.cOAPI)newInstance;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (attempt == 10)
                        {
                            throw ex;
                        }
                    }
                    if (ETABSObject != null)
                    {
                        ret = ETABSObject.ApplicationStart();
                    }
                }

        }
    }
}
