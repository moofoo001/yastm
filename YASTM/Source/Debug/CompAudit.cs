using System;
using RimWorld;
using Verse;

namespace YASTM
{
    [StaticConstructorOnStartup]
    public static class CompAudit
    {
        static CompAudit()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.comps == null) continue;
                foreach (var props in def.comps)
                {
                    if (props == null)
                    {
                        Log.Error($"[YASTM][AUDIT] {def.defName}: null CompProperties in XML.");
                        continue;
                    }
                    if (props.compClass == null)
                    {
                        Log.Error($"[YASTM][AUDIT] {def.defName}: CompProperties '{props.GetType().Name}' has null compClass.");
                        continue;
                    }
                    if (!typeof(ThingComp).IsAssignableFrom(props.compClass))
                    {
                        Log.Error($"[YASTM][AUDIT] {def.defName}: compClass '{props.compClass.FullName}' is not a ThingComp.");
                    }
                    // häufigster Fehler: direkt ThingComp gesetzt (statt spez. Comp)
                   if (props.compClass == typeof(ThingComp))
                    {
                        Log.Error($"[YASTM][AUDIT] {def.defName}: compClass is plain ThingComp; propsType={props.GetType().FullName}");
                    }
                }
            }
        }
    }
}

