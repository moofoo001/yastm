// File: Source/Comps/CompOpsConsole_Alerts.cs
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    // Hinweis: Diese Datei macht KEINE statischen Aufrufe mehr an MapComponent_AlertPanel.TryStart*,
    // sondern arbeitet direkt über die Instanz der MapComponent. So läuft es auch,
    // wenn deine MapComponent_AlertPanel keine statischen Wrapper definiert.
    public partial class CompOpsConsole : ThingComp
    {
        // Falls du auf diesem ThingDef CompProperties_AlertPanelGizmo hinterlegt hast:
        public CompProperties_AlertPanelGizmo PropsAlerts => props as CompProperties_AlertPanelGizmo;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            if (parent?.Map == null || PropsAlerts == null || parent.Faction != Faction.OfPlayer)
                yield break;

            var map = parent.Map;
            var mc  = map.GetComponent<MapComponent_AlertPanel>();

            // Wenn die MapComponent noch nicht existiert, hier früh raus (verhindert NullRefs)
            if (mc == null)
                yield break;

            // --- Red Alert ---
            bool redOnCd = Find.TickManager.TicksGame < mc.NextAllowedTickRed;
            string redLabel = redOnCd
                ? $"Red Alert (CD {(mc.NextAllowedTickRed - Find.TickManager.TicksGame) / 60}s)"
                : "Red Alert";

            yield return new Command_Action
            {
                defaultLabel = redLabel,
                defaultDesc  = "Colony-weiter roter Alarm – Sirene, Buffs, Timer.",
                icon         = ContentFinder<Texture2D>.Get("Things/UI/Icons/Gizmos/RedAlert", true),
                action       = () =>
                {
                    mc.StartRedAlert(
                        PropsAlerts.redDurationTicks,
                        PropsAlerts.redCooldownTicks,
                        PropsAlerts.blinkSeconds
                    );
                }
            };

            // --- Yellow Alert ---
            bool yellowOnCd = Find.TickManager.TicksGame < mc.NextAllowedTickYellow;
            string yellowLabel = yellowOnCd
                ? $"Yellow Alert (CD {(mc.NextAllowedTickYellow - Find.TickManager.TicksGame) / 60}s)"
                : "Yellow Alert";

            yield return new Command_Action
            {
                defaultLabel = yellowLabel,
                defaultDesc  = "Erhöhter Bereitschaftsstatus – Sirene, Buffs, Timer.",
                icon         = ContentFinder<Texture2D>.Get("Things/UI/Icons/Gizmos/YellowAlert", true),
                action       = () =>
                {
                    mc.StartYellowAlert(
                        PropsAlerts.yellowDurationTicks,
                        PropsAlerts.yellowCooldownTicks,
                        PropsAlerts.blinkSeconds
                    );
                }
            };
        }
    }
}
