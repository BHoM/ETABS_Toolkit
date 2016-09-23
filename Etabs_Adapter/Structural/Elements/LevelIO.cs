using BHoM.Structural.Elements;
using ETABS2015;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etabs_Adapter.Structural.Elements
{
    public class LevelIO
    {
        public static bool SetLevels(cOAPI Etabs, List<Storey> levels, out List<string> ids)
        {
            ids = new List<string>();
            if (levels != null && levels.Count > 0)
            {
                cSapModel SapModel = Etabs.SapModel;
                string[] names = new string[levels.Count - 1];
                double[] storyElevations = new double[levels.Count];
                double[] storyHeights = new double[levels.Count - 1];
                bool[] masterStory = new bool[levels.Count - 1];
                string[] similiar = new string[levels.Count - 1];
                bool[] spliceAbove = new bool[levels.Count - 1];
                double[] spliceHeight = new double[levels.Count - 1];

            
                levels.Sort(delegate (Storey s1, Storey s2)
                {
                    return s1.Elevation.CompareTo(s2.Elevation);
                });

                storyElevations[0] = levels[0].Elevation;
                for (int i = 1; i < levels.Count; i++)
                {
                    names[i - 1] = levels[i].Name;
                    storyElevations[i] = levels[i].Elevation;
                }

                ids.Add("Base");
                ids.AddRange(names.ToList());
                SapModel.Story.SetStories(names, storyElevations, storyHeights, masterStory, similiar, spliceAbove, spliceHeight);
            }
            return true;
        }
    }
}
