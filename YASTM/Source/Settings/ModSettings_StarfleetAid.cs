using Verse;

namespace YASTM
{
    public class YASTM_Settings : ModSettings
    {
        public float AidCooldownDays = 10f;
        public int   AidSilverCost   = 200;
        public int   AidGoodwillCost = 6;
        public int   AidMinGoodwill  = 40;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref AidCooldownDays, "AidCooldownDays", 10f);
            Scribe_Values.Look(ref AidSilverCost,   "AidSilverCost",   200);
            Scribe_Values.Look(ref AidGoodwillCost, "AidGoodwillCost", 6);
            Scribe_Values.Look(ref AidMinGoodwill,  "AidMinGoodwill",  40);
        }
    }
}

