using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    // This partial comp supplies Red/Yellow Alert buttons via the Ops console.
    public partial class CompOpsConsole : ThingComp
    {
        public CompProperties_AlertPanelGizmo PropsAlerts => props as CompProperties_AlertPanelGizmo;

        private static int Safe(int val, int fallback) => val > 0 ? val : fallback;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            if (parent?.Map == null || parent.Faction != Faction.OfPlayer) yield break;

            var map = parent.Map;
            var mc  = map.GetComponent<MapComponent_AlertPanel>();
            if (mc == null) yield break;

            // Fallbacks if XML props are missing/zero
            int redDur     = Safe(PropsAlerts?.redDurationTicks    ?? 0,  9000);  // 2.5 in-game hours
            int redCd      = Safe(PropsAlerts?.redCooldownTicks    ?? 0, 60000);  // 1 in-game day
            int yellowDur  = Safe(PropsAlerts?.yellowDurationTicks ?? 0,  6000);  // 1.7 in-game hours
            int yellowCd   = Safe(PropsAlerts?.yellowCooldownTicks ?? 0, 30000);  // 0.5 in-game day
            int blinkSecs  = Safe(PropsAlerts?.blinkSeconds        ?? 0,     10);

            // Red Alert
            bool redOnCd = Find.TickManager.TicksGame < mc.NextAllowedTickRed;
            string redLabel = redOnCd
                ? $"Red Alert (CD {(mc.NextAllowedTickRed - Find.TickManager.TicksGame).ToStringTicksToPeriod()})"
                : "Red Alert";

            yield return new Command_Action
            {
                defaultLabel = redLabel,
                defaultDesc  = "Colony-wide red alert — siren, buffs, timer.",
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RedAlert", true),
                action       = () =>
                {
                    mc.StartRedAlert(redDur, redCd, blinkSecs);
                    Messages.Message("Red Alert engaged.", MessageTypeDefOf.NeutralEvent);
                }
            };

            // Yellow Alert
            bool yellowOnCd = Find.TickManager.TicksGame < mc.NextAllowedTickYellow;
            string yellowLabel = yellowOnCd
                ? $"Yellow Alert (CD {(mc.NextAllowedTickYellow - Find.TickManager.TicksGame).ToStringTicksToPeriod()})"
                : "Yellow Alert";

            yield return new Command_Action
            {
                defaultLabel = yellowLabel,
                defaultDesc  = "Heightened readiness — siren, buffs, timer.",
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/YellowAlert", true),
                action       = () =>
                {
                    mc.StartYellowAlert(yellowDur, yellowCd, blinkSecs);
                    Messages.Message("Yellow Alert engaged.", MessageTypeDefOf.NeutralEvent);
                }
            };
        }
    }
}
