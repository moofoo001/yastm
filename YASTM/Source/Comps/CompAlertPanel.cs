using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_AlertPanel : CompProperties
    {
        public int redDurationTicks = 18000;
        public int yellowDurationTicks = 9000;
        public int redCooldownTicks = 90000;
        public int yellowCooldownTicks = 45000;
        public int blinkSeconds = 3;
        
        // Neu für die Grafik-Steuerung
        public int pulseInterval = 60; // Schneller Takt (1 Sekunde) für Alarm
        public float pulseRadius = 3.0f;

        public CompProperties_AlertPanel()
        {
            this.compClass = typeof(CompAlertPanel);
        }
    }

    // Wir brauchen hier kein StaticConstructorOnStartup mehr, da Flecks das regeln
    public class CompAlertPanel : ThingComp
    {
        public CompProperties_AlertPanel Props => (CompProperties_AlertPanel)props;

        private int nextPulseTick = 0;

        public override void CompTick()
        {
            base.CompTick();

            if (parent.Map == null) return;

            // 1. Status holen
            var mc = parent.Map.GetComponent<MapComponent_AlertPanel>();
            if (mc == null || !mc.IsRedAlertActive) return;

            // 2. Pulsieren (nur bei Red Alert)
            if (Find.TickManager.TicksGame >= nextPulseTick)
            {
                TriggerRedPulse();
                nextPulseTick = Find.TickManager.TicksGame + Props.pulseInterval;
            }
        }

        private void TriggerRedPulse()
        {
            // XML Def laden
            FleckDef pulseFleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_RedAlertPulse");
            
            // Fallback
            if (pulseFleck == null) pulseFleck = FleckDefOf.PsycastAreaEffect;

            // Effekt feuern
            FleckMaker.Static(parent.TrueCenter(), parent.Map, pulseFleck, Props.pulseRadius);
        }

        // --- GIZMOS (Buttons bleiben gleich) ---
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;
            var map = parent.Map;
            if (map == null) yield break;
            
            var mc = map.GetComponent<MapComponent_AlertPanel>();

            // Red Alert
            yield return new Command_Action
            {
                defaultLabel = mc != null && mc.IsRedOnCooldown ? $"Red Alert (CD {mc.ArmRedCooldownSeconds()}s)" : "Red Alert",
                defaultDesc  = "Colony-wide red alert.",
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RedAlert", false),
                action       = () => mc?.StartRedAlert(Props.redDurationTicks, Props.redCooldownTicks, Props.blinkSeconds)
            };

            // Yellow Alert
            yield return new Command_Action
            {
                defaultLabel = mc != null && mc.IsYellowOnCooldown ? $"Yellow Alert (CD {mc.ArmYellowCooldownSeconds()}s)" : "Yellow Alert",
                defaultDesc  = "Heightened awareness.",
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/YellowAlert", false),
                action       = () => mc?.StartYellowAlert(Props.yellowDurationTicks, Props.yellowCooldownTicks, Props.blinkSeconds)
            };
        }
    }
}