using System.Collections.Generic;
using Verse;

namespace YASTM
{
    public class RankVisualExtension : DefModExtension
    {
        // Der Pfad zu den Texturen (wird im XML definiert)
        public string rankTexPath;
        
        // Die Liste der Ränge
        public List<RankData> ranks;
    }

    public class RankData
    {
        public int degree;      // z.B. 1
        public string texName;  // z.B. "Ensign"
        
        // Optional: Falls der DefName mal ganz anders heißt als der texName
        public string specificDefName; 
    }
}