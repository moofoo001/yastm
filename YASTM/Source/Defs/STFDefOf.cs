using RimWorld;
using Verse;

namespace StarTrekFactions
{
    [DefOf]
    public static class STFDefOf
    {
        // Things
        public static ThingDef ST_Obelisk_A;
        public static ThingDef ST_Obelisk_B;
        public static ThingDef ST_ComBeacon;
        public static ThingDef ST_Subspace_Scanner; 
        // Job
          public static JobDef ST_ScanObelisk;

        // WorkType (vanilla)
        public static WorkTypeDef Research;

        // Letters
        public static LetterDef ST_AprilComms_Letter;

        static STFDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(STFDefOf));
        }
    }
}

