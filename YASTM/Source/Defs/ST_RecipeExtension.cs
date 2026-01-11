using Verse;
using RimWorld;

namespace YASTM
{
    public class ST_RecipeExtension : DefModExtension
    {

        public TraitDef requiredTrait;
        public int requiredDegree = -999;
        public bool mustHaveTrait = true;

        // Apparel to reward upon successful completion of the recipe
        public ThingDef rewardApparel;
        
        // Apparel tag to remove when applying the rewardApparel
        public string removeApparelWithTag = "ST_RankPip";
    }
}