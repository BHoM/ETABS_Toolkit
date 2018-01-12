using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.Adapter.ETABS;

namespace ETABS_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ETABSAdapter app = new ETABSAdapter();

            TestPushBars(app);
        }

        private static void TestPushBars(ETABSAdapter app)
        {

        }

        private static void TestPullBars(ETABSAdapter app)
        {

        }
    }
}
