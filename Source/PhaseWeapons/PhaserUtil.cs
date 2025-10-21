using Verse;

namespace ST.PhaseWeapons
{
public static class PhaserUtil
{
    public static CompPhaserMode GetPhaserComp(ThingWithComps gear)
    {
        if (gear == null) return null;
        CompPhaserMode fallback = null;
        var comps = gear.AllComps;
        if (comps != null)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompPhaserMode pm)
                {
                    var p = pm.Props;
                    if (p != null && (p.projectileKill != null || p.projectileStun != null || p.projectileOvercharge != null))
                        return pm;           
                    fallback ??= pm;           
                }
            }
        }
        return fallback ?? gear.TryGetComp<CompPhaserMode>();
    }
}
}
