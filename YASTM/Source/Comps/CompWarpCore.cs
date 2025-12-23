using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_WarpCore : CompProperties
    {
        public float explosionRadius = 25f; 
        public float safeTemperatureMax = 60f; 
        public int damagePerTick = 1; 
        
        // Grafik-Einstellungen
        public int pulseInterval = 180; 
        public float pulseRadius = 3.5f;
        
        public CompProperties_WarpCore()
        {
            this.compClass = typeof(CompWarpCore);
        }
    }

    public class CompWarpCore : ThingComp
    {
        public CompProperties_WarpCore Props => (CompProperties_WarpCore)props;

        private bool ejected = false;
        private int instabilityCounter = 0;
        
        // NEU: Ejection Sequenz Logik
        private bool ejectionSequenceActive = false;
        private int ejectionCountdown = 300; // 5 Sekunden (bei 60 Ticks/Sekunde)
        
        // Timer für Grafik
        private int nextPulseTick = 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ejected, "ejected", false);
            Scribe_Values.Look(ref instabilityCounter, "instabilityCounter", 0);
            
            // Status speichern
            Scribe_Values.Look(ref ejectionSequenceActive, "ejectionSequenceActive", false);
            Scribe_Values.Look(ref ejectionCountdown, "ejectionCountdown", 300);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!IsActive()) return;

            // --- 1. EJECTION SEQUENZ (Der Countdown) ---
            if (ejectionSequenceActive)
            {
                ejectionCountdown--;
                
                // Warnung alle Sekunde
                if (ejectionCountdown % 60 == 0)
                {
                    MoteMaker.ThrowText(parent.DrawPos, parent.Map, $"EJECTING IN {ejectionCountdown / 60}...", Color.red);
                }

                // BOOM / Eject wenn Zeit abgelaufen
                if (ejectionCountdown <= 0)
                {
                    DoEject();
                }
                
                // Aggressiver roter Puls während des Countdowns
                if (Find.TickManager.TicksGame % 20 == 0) // Schnelles Blinken
                {
                     FleckMaker.Static(parent.TrueCenter(), parent.Map, FleckDefOf.PsycastAreaEffect, 5f);
                }
                
                return; // Keine weitere Hitze-Berechnung während der Sequenz
            }

            // --- 2. Normaler Betrieb (Puls) ---
            if (Find.TickManager.TicksGame >= nextPulseTick)
            {
                TriggerWarpPulse();
                nextPulseTick = Find.TickManager.TicksGame + Props.pulseInterval;
            }

            // --- 3. Temperatur Check ---
            float roomTemp = parent.AmbientTemperature;
            if (roomTemp > Props.safeTemperatureMax)
            {
                instabilityCounter++;
                if (instabilityCounter > 600) 
                {
                    parent.TakeDamage(new DamageInfo(DamageDefOf.Burn, Props.damagePerTick, 0f, -1f, parent));
                    instabilityCounter = 0;
                    if (Rand.Value < 0.2f) 
                        Messages.Message("Warning: Warp Core Overheating!", parent, MessageTypeDefOf.NegativeEvent);
                }
            }
            else
            {
                if (instabilityCounter > 0) instabilityCounter--;
            }
        }

        // Die eigentliche Auswurf-Funktion
        private void DoEject()
        {
            ejected = true;
            ejectionSequenceActive = false; // Reset
            
            Messages.Message("CORE EJECTED!", MessageTypeDefOf.PositiveEvent);
            
            // Explosion verhindern, da kontrollierter Auswurf
            // Wir zerstören das Gebäude einfach sicher
            parent.Destroy(DestroyMode.KillFinalize); 
        }

        private void TriggerWarpPulse()
        {
            if (parent.Map == null) return;
            FleckDef pulseFleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_WarpPulse");
            if (pulseFleck == null) pulseFleck = FleckDefOf.PsycastAreaEffect;
            FleckMaker.Static(parent.TrueCenter(), parent.Map, pulseFleck, Props.pulseRadius);
        }

        private bool IsActive()
        {
            var power = parent.TryGetComp<CompPowerTrader>();
            return (power != null && power.PowerOn);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            // HIER IST IHRE GEWÜNSCHTE LOGIK
            // Wir nutzen Command_Toggle statt Command_Action
            yield return new Command_Toggle
            {
                defaultLabel = ejectionSequenceActive ? "ABORT EJECTION" : "EJECT WARP CORE",
                defaultDesc = "EMERGENCY: Initiates core ejection sequence.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/EjectCore", false),
                
                // --- DIE FARB-LOGIK ---
                // Wenn Aktiv (gedrückt) -> ROT
                // Wenn Inaktiv (nicht gedrückt) -> WEISS
                defaultIconColor = ejectionSequenceActive ? Color.red : Color.white,
                
                isActive = () => ejectionSequenceActive,
                toggleAction = () => 
                {
                    // Schalter umlegen
                    ejectionSequenceActive = !ejectionSequenceActive;
                    
                    if (ejectionSequenceActive)
                    {
                        // Start Sequenz
                        ejectionCountdown = 300; // Reset Timer auf 5 Sek
                        Messages.Message("EJECTION SEQUENCE INITIATED!", parent, MessageTypeDefOf.ThreatSmall);
                    }
                    else
                    {
                        // Abbrechen
                        Messages.Message("Ejection sequence aborted.", parent, MessageTypeDefOf.NeutralEvent);
                    }
                }
            };
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            // Wenn zerstört wurde OHNE dass 'ejected' true ist (z.B. durch Beschuss), dann BUMM
            if (!ejected && mode == DestroyMode.KillFinalize)
            {
                var refuelable = parent.TryGetComp<CompRefuelable>();
                if (refuelable != null && refuelable.HasFuel)
                {
                    if (previousMap != null)
                    {
                        Messages.Message("WARP CORE BREACH DETECTED!", MessageTypeDefOf.ThreatBig);
                        GenExplosion.DoExplosion(
                            center: parent.Position, 
                            map: previousMap, 
                            radius: Props.explosionRadius, 
                            damType: DamageDefOf.Bomb, 
                            instigator: parent,
                            damAmount: -1, 
                            armorPenetration: -1f, 
                            explosionSound: null, 
                            weapon: null, 
                            projectile: null, 
                            intendedTarget: null, 
                            postExplosionSpawnThingDef: null, 
                            postExplosionSpawnChance: 0f, 
                            postExplosionSpawnThingCount: 1, 
                            postExplosionGasType: null,
                            applyDamageToExplosionCellsNeighbors: false
                        );
                    }
                }
            }
        }
    }
}