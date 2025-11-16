using RimWorld;
using Verse;

namespace YASTM
{
    [DefOf]
    public static class ST_SoundDefOf
    {
        public static SoundDef ST_RedAlert_SirenLoop;
        public static SoundDef ST_YellowAlert_SirenLoop;

        static ST_SoundDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ST_SoundDefOf));
        }
    }
}
