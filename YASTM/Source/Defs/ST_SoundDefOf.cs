using RimWorld;
using Verse;

namespace YASTM
{
    [DefOf]
    public static class ST_SoundDefOf
    {
        // Red Alert
        public static SoundDef ST_Sound_RedAlert;       // OneShot
        public static SoundDef ST_Sound_RedAlert_Loop;  // Loop

        // Yellow Alert
        public static SoundDef ST_Sound_YellowAlert;      // OneShot
        public static SoundDef ST_Sound_YellowAlert_Loop; // Loop

        public static SoundDef ST_Transporter_Beam;
        public static SoundDef ST_Shuttle_Launch;

        static ST_SoundDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ST_SoundDefOf));
        }
    }
}

