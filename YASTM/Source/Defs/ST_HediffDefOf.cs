using RimWorld;
using Verse;

namespace YASTM
{
    [DefOf]
    public static class ST_HediffDefOf
    {
        // Alert Buffs
        public static HediffDef ST_Alert_RedState;
        public static HediffDef ST_Alert_YellowState;

        static ST_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ST_HediffDefOf));
        }
    }
}