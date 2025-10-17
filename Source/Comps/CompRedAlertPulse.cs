using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class CompProperties_RedAlertPulse : CompProperties
    {
        public int durationTicks = 15000;   // 4h
        public int cooldownTicks = 60000;   // 1 Tag
        public CompProperties_RedAlertPulse() { compClass = typeof(CompUseEffect_RedAlertPulse); }
    }

    public class CompUseEffect_RedAlertPulse : CompUseEffect
    {
        public CompProperties_RedAlertPulse Props => (CompProperties_RedAlertPulse)props;

        public override void DoEffect(Pawn user)
        {
            var map = user?.Map;
            if (map == null) return;

            var mc = map.GetComponent<MapComponent_RedAlert>();
            int now = Find.TickManager.TicksGame;
            if (mc != null && now < mc.NextAllowedTick)
            {
                int rem = mc.NextAllowedTick - now;
                Messages.Message("Red Alert uplink recharging: " + rem.ToStringTicksToPeriod(), MessageTypeDefOf.RejectInput);
                return;
            }

            var pawns = map.mapPawns.FreeColonistsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                Messages.Message("No colonists present.", MessageTypeDefOf.RejectInput);
                return;
            }

            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ST_RedAlert");
            if (hediffDef == null)
            {
                Messages.Message("Missing hediff: ST_RedAlert", MessageTypeDefOf.RejectInput);
                return;
            }

            foreach (var p in pawns)
            {
                var h = p.health?.AddHediff(hediffDef) as HediffWithComps;
                if (h != null)
                {
                    var d = h.TryGetComp<HediffComp_Disappears>();
                    if (d != null) d.ticksToDisappear = Props.durationTicks;
                }
                // kleines visuelles Feedback
                MoteMaker.ThrowText(p.DrawPos, map, "RED ALERT", Color.red, 1.5f);
            }

            // simple sound hint (falls du ein eigenes SoundDef hast, hier ersetzen)
            Messages.Message("Red Alert engaged.", MessageTypeDefOf.PositiveEvent);

            if (mc != null) mc.NextAllowedTick = now + Props.cooldownTicks;
        }
    }
}
