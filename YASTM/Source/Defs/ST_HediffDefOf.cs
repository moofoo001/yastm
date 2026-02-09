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

        // Cloaking Field
        public static HediffDef ST_CloakingField;

        static ST_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ST_HediffDefOf));
        }
    }
}