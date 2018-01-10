using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Etabs_Adapter;
using BHoM;


namespace TestingConsole
{
    class TestingProgram
    {
        static void Main()
        {
            Console.WriteLine("file path to ETABS file you want to use");
            string filePath = @"C:\Users\mhenriks\Desktop\Expo_local\ETABSFiles\SS\columnForDebug.EDB";// Console.ReadLine();


            Etabs_Adapter.Structural.Interface.EtabsAdapter adapt = new Etabs_Adapter.Structural.Interface.EtabsAdapter(filePath);
            List<BHoM.Structural.Loads.ICase> cList = new List<BHoM.Structural.Loads.ICase>();

            adapt.GetLoadcases(out cList);
            List<BHoM.Structural.Loads.Loadcase> lcList = new List<BHoM.Structural.Loads.Loadcase>();
            lcList.Add(cList[0] as BHoM.Structural.Loads.Loadcase);
            lcList.Add(cList[1] as BHoM.Structural.Loads.Loadcase);
            lcList.Add(cList[2] as BHoM.Structural.Loads.Loadcase);


            List<BHoM.Structural.Loads.ILoad> loads = new List<BHoM.Structural.Loads.ILoad>();

            adapt.GetLoads(out loads, lcList);



        }
    }
}
