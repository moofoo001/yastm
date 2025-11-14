// File: Source/Comps/CompAlertPanelGizmo.cs
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_AlertPanelGizmo : CompProperties
    {
        public int redDurationTicks = 18000;
        public int yellowDurationTicks = 9000;
        public int redCooldownTicks = 90000;
        public int yellowCooldownTicks = 45000;
        public int blinkSeconds = 3;

        public CompProperties_AlertPanelGizmo()
        {
            compClass = typeof(CompAlertPanelGizmo);
        }
    }

    public class CompAlertPanelGizmo : ThingComp
    {
        public CompProperties_AlertPanelGizmo Props => (CompProperties_AlertPanelGizmo)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;
            var map = parent.Map;
            var mc = map.GetComponent<MapComponent_AlertPanel>();

            // Red Alert
            yield return new Command_Action
            {
                defaultLabel = mc != null && mc.IsRedOnCooldown ? $"Red Alert (CD {mc.ArmRedCooldownSeconds()}s)" : "Red Alert",
                defaultDesc  = "Colony-weiter roter Alarm – Sirene, Buffs und Timer.",
                icon         = ContentFinder<Texture2D>.Get("Things/UI/Icons/Gizmos/RedAlert", false),
                action       = () =>
                    map.GetComponent<MapComponent_AlertPanel>()
                       .StartRedAlert(Props.redDurationTicks, Props.redCooldownTicks, Props.blinkSeconds)
            };

            // Yellow Alert
            yield return new Command_Action
            {
                defaultLabel = mc != null && mc.IsYellowOnCooldown ? $"Yellow Alert (CD {mc.ArmYellowCooldownSeconds()}s)" : "Yellow Alert",
                defaultDesc  = "Erhöhter Bereitschaftsstatus – Sirene, Buffs und Timer.",
                icon         = ContentFinder<Texture2D>.Get("Things/UI/Icons/Gizmos/YellowAlert", false),
                action       = () =>
                    map.GetComponent<MapComponent_AlertPanel>()
                       .StartYellowAlert(Props.yellowDurationTicks, Props.yellowCooldownTicks, Props.blinkSeconds)
            };
        }
    }
}
