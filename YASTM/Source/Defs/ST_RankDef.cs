using Verse;
using RimWorld;

namespace YASTM
{
    public class ST_RankDef : Def
    {
        public int level;
        
        // FIX: Dieses Feld fehlte, wird aber vom UI-Patch gesucht!
        public string iconPath; 
        
        // Optional: Falls Sie Stats binden wollen
        public float commonality = 1f;
    }
}