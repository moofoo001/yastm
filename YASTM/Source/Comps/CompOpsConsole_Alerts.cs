using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    // This partial comp supplies Red/Yellow Alert buttons via the Ops console.
    public partial class CompOpsConsole : ThingComp
    {

    public CompProperties_AlertPanel PropsAlerts => props as CompProperties_AlertPanel;

        private static int Safe(int val, int fallback) => val > 0 ? val : fallback;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Base Gizmos
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            if (parent?.Map == null || parent.Faction != Faction.OfPlayer) yield break;

            var map = parent.Map;
            var mc  = map.GetComponent<MapComponent_AlertPanel>();
            if (mc == null) yield break;

            // Fetch Durations
            int redDur     = Safe(PropsAlerts?.redDurationTicks    ?? 0,  9000);
            int redCd      = Safe(PropsAlerts?.redCooldownTicks    ?? 0, 60000);
            int yellowDur  = Safe(PropsAlerts?.yellowDurationTicks ?? 0,  6000);
            int yellowCd   = Safe(PropsAlerts?.yellowCooldownTicks ?? 0, 30000);
            int blinkSecs  = Safe(PropsAlerts?.blinkSeconds        ?? 0,     10);

            // --- RED ALERT BUTTON ---
            if (mc.redAlertTicksLeft <= 0) 
            {
                bool redOnCd = Find.TickManager.TicksGame < mc.NextAllowedTickRed;
                string redLabel = redOnCd
                    ? $"Red Alert (CD {(mc.NextAllowedTickRed - Find.TickManager.TicksGame).ToStringTicksToPeriod()})"
                    : "Red Alert";

                var cmd = new Command_Action
                {
                    defaultLabel = redLabel,
                    defaultDesc  = "Colony-wide red alert.",
                    icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RedAlert", true),
                    action       = () => mc.StartRedAlert(redDur, redCd, blinkSecs)
                };
                if (redOnCd) cmd.Disable("On Cooldown");
                yield return cmd;
            }

            // --- YELLOW ALERT BUTTON ---
            if (mc.yellowAlertTicksLeft <= 0 && mc.redAlertTicksLeft <= 0)
            {
                 bool yellowOnCd = Find.TickManager.TicksGame < mc.NextAllowedTickYellow;
                 string yellowLabel = yellowOnCd
                    ? $"Yellow Alert (CD {(mc.NextAllowedTickYellow - Find.TickManager.TicksGame).ToStringTicksToPeriod()})"
                    : "Yellow Alert";
                
                var cmd = new Command_Action
                {
                    defaultLabel = yellowLabel,
                    defaultDesc  = "Heightened readiness.",
                    icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/YellowAlert", true),
                    action       = () => mc.StartYellowAlert(yellowDur, yellowCd, blinkSecs)
                };
                if (yellowOnCd) cmd.Disable("On Cooldown");
                yield return cmd;
            }

            // --- CONDITION GREEN BUTTON (CANCEL) ---
            if (mc.redAlertTicksLeft > 0 || mc.yellowAlertTicksLeft > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Condition Green",
                    defaultDesc  = "Stand down. Cancel alert.",
                    icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/GreenAlert", true), 
                    action       = () => mc.EndAlert()
                };
            }
        }
    }
}