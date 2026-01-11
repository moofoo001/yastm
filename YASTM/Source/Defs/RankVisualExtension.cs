using System.Collections.Generic;
using Verse;

namespace YASTM
{
    public class RankVisualExtension : DefModExtension
    {

        public string rankTexPath;
        

        public List<RankData> ranks;
    }

    public class RankData
    {
        public int degree;      
        public string texName;  // eg "Ensign"
        
        public string specificDefName; 
    }
}