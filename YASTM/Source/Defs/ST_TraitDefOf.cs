using RimWorld;
using Verse;

namespace YASTM
{
    [DefOf]
    public static class ST_TraitDefOf
    {
        public static TraitDef ST_TransporterEngineer;

        static ST_TraitDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ST_TraitDefOf));
        }
    }
}