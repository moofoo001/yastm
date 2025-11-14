using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    public partial class CompOpsConsole : ThingComp
    {
        // nutzt dieselben Werte wie das AlertPanel
        public CompProperties_AlertPanelGizmo PropsAlerts => props as CompProperties_AlertPanelGizmo;

        private IEnumerable<Gizmo> AlertGizmos()
        {
            var map = parent.Map;
            if (map == null || PropsAlerts == null) yield break;

            yield return new Command_Action
            {
                defaultLabel = "Red Alert",
                defaultDesc  = "Trigger shipwide red alert.",
                icon         = ST_UITex.RedAlert,
                action       = () =>
                    MapComponent_AlertPanel.TryStartRedAlert(map,
                        PropsAlerts.redDurationTicks,
                        PropsAlerts.redCooldownTicks,
                        PropsAlerts.blinkSeconds)
            };

            yield return new Command_Action
            {
                defaultLabel = "Yellow Alert",
                defaultDesc  = "Trigger shipwide yellow alert.",
                icon         = ST_UITex.YellowAlert,
                action       = () =>
                    MapComponent_AlertPanel.TryStartYellowAlert(map,
                        PropsAlerts.yellowDurationTicks,
                        PropsAlerts.yellowCooldownTicks,
                        PropsAlerts.blinkSeconds)
            };
        }
    }
}
