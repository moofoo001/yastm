using System.Collections.Generic;
using Verse;

namespace YASTM
{
    public class RankVisualExtension : DefModExtension
    {

        public string rankTexPath; 
        


        public List<RankGraphicData> ranks;



        public string texName; 
    }

    public class RankGraphicData
    {
        public int degree;
        public string texName;
    }
}