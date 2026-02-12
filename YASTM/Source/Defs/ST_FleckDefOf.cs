using RimWorld;
using Verse;

namespace YASTM
{
    [DefOf]
    public static class ST_FleckDefOf
    {
        public static FleckDef ST_Fleck_BadgeyOverlay; 

        static ST_FleckDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ST_FleckDefOf));
        }
    }
}