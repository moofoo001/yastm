using Verse;
using RimWorld;

namespace YASTM
{
    public class ST_RecipeExtension : DefModExtension
    {
        // Existierende Felder
        public TraitDef requiredTrait;
        public int requiredDegree = -999;
        public bool mustHaveTrait = true;

        // NEU: Das Item, das man bekommt (z.B. ST_Apparel_Pip_Ensign)
        public ThingDef rewardApparel;
        
        // NEU: Tag, um alte Pips zu finden und zu entfernen (z.B. "ST_RankPip")
        public string removeApparelWithTag = "ST_RankPip";
    }
}