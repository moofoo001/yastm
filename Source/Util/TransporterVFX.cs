// Source/Util/TransporterVFX.cs
using RimWorld;
using Verse;

namespace YASTM
{
    public static class TransporterVFX
    {
        public static void PlayBeam(Map map, IntVec3 pos)
        {
            if (map == null) return;
            var data = FleckMaker.GetDataStatic(pos.ToVector3Shifted(), map, FleckDefOf.PsycastAreaEffect, 1.25f);
            data.rotation = Rand.Range(0f, 360f);
            map.flecks.CreateFleck(data);
        }
    }
}
