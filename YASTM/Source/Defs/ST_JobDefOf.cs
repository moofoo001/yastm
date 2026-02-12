using RimWorld;
using Verse;

namespace YASTM
{
    [DefOf]
    public static class ST_JobDefOf
    {
        public static JobDef ST_Job_ManBridgeStation; // for the bridge
        public static JobDef ST_Job_PurgeBadgey;      // for badgey

        static ST_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ST_JobDefOf));
        }
    }
}