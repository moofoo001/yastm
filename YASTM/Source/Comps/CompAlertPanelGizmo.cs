using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    public class CompProperties_AlertPanelGizmo : CompProperties
    {
        public int redDurationTicks = 60 * 20;    // 20s
        public int redCooldownTicks = 60 * 120;   // 2 min
        public int yellowDurationTicks = 60 * 20;
        public int yellowCooldownTicks = 60 * 90;
        public int blinkSeconds = 3;              // neuer 4. Parameter für TryStart*

        public CompProperties_AlertPanelGizmo() => compClass = typeof(CompAlertPanelGizmo);
    }

    public class CompAlertPanelGizmo : ThingComp
    {
        public CompProperties_AlertPanelGizmo Props => (CompProperties_AlertPanelGizmo)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var map = parent.Map;
            if (map == null) yield break;

            // Buttons nie „hart“ disablen – TryStart* meldet selbst Cooldown/Erfolg
            yield return new Command_Action
            {
                defaultLabel = "Red Alert",
                defaultDesc  = "Trigger shipwide red alert.",
                icon         = ST_UITex.RedAlert,
                action       = () =>
                    MapComponent_AlertPanel.TryStartRedAlert(map, Props.redDurationTicks, Props.redCooldownTicks, Props.blinkSeconds)
            };

            yield return new Command_Action
            {
                defaultLabel = "Yellow Alert",
                defaultDesc  = "Trigger shipwide yellow alert.",
                icon         = ST_UITex.YellowAlert,
                action       = () =>
                    MapComponent_AlertPanel.TryStartYellowAlert(map, Props.yellowDurationTicks, Props.yellowCooldownTicks, Props.blinkSeconds)
            };
        }
    }
}
