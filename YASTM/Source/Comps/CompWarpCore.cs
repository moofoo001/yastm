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
        
        // NEU: Ejection sequence
        private bool ejectionSequenceActive = false;
        private int ejectionCountdown = 300; // 5 seconds
        
        // Pulsing
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

            // --- 1. Ejection Sequence ---
            if (ejectionSequenceActive)
            {
                ejectionCountdown--;
                

                if (ejectionCountdown % 60 == 0)
                {
                    MoteMaker.ThrowText(parent.DrawPos, parent.Map, $"EJECTING IN {ejectionCountdown / 60}...", Color.red);
                }

                // BOOM
                if (ejectionCountdown <= 0)
                {
                    DoEject();
                }
                
                // pulsing effect during countdown
                if (Find.TickManager.TicksGame % 20 == 0) 
                {
                     FleckMaker.Static(parent.TrueCenter(), parent.Map, FleckDefOf.PsycastAreaEffect, 5f);
                }
                
                return; // Skip rest of tick while ejecting
            }

            // --- 2. Warp Pulse ---
            if (Find.TickManager.TicksGame >= nextPulseTick)
            {
                TriggerWarpPulse();
                nextPulseTick = Find.TickManager.TicksGame + Props.pulseInterval;
            }

            // --- 3. Overheat Check ---
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

        // --- EJECTION SEQUENCE LOGIC ---
        private void DoEject()
        {
            ejected = true;
            ejectionSequenceActive = false; // Reset
            
            Messages.Message("CORE EJECTED!", MessageTypeDefOf.PositiveEvent);
            
            // safely destroy the core
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


            // EJECTION SEQUENCE GIZMO
            yield return new Command_Toggle
            {
                defaultLabel = ejectionSequenceActive ? "ABORT EJECTION" : "EJECT WARP CORE",
                defaultDesc = "EMERGENCY: Initiates core ejection sequence.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/EjectCore", false),
                

                // red when active
                // white when inactive
                defaultIconColor = ejectionSequenceActive ? Color.red : Color.white,
                
                isActive = () => ejectionSequenceActive,
                toggleAction = () => 
                {
                    // Schalter umlegen
                    ejectionSequenceActive = !ejectionSequenceActive;
                    
                    if (ejectionSequenceActive)
                    {
                        // Start countdown
                        ejectionCountdown = 300; // Reset Timer
                        Messages.Message("EJECTION SEQUENCE INITIATED!", parent, MessageTypeDefOf.ThreatSmall);
                    }
                    else
                    {
                        // abort
                        Messages.Message("Ejection sequence aborted.", parent, MessageTypeDefOf.NeutralEvent);
                    }
                }
            };
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            // explosion check
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