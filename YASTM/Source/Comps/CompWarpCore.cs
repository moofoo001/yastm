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
        
        // Neu: Konfiguration für den Puls
        public int pulseInterval = 180; // Alle 3 Sekunden (bei 60 TPS)
        public float pulseRadius = 3.5f; // Größe des Effekts
        
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
        
        // Timer für den visuellen Effekt
        private int nextPulseTick = 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ejected, "ejected", false);
            Scribe_Values.Look(ref instabilityCounter, "instabilityCounter", 0);
        }

        public override void CompTick()
        {
            base.CompTick();
            
            // 1. Ist der Reaktor überhaupt aktiv? (Strom + Fuel)
            if (!IsActive()) return;

            // 2. Visueller Puls (Logik adaptiert vom Obelisken)
            if (Find.TickManager.TicksGame >= nextPulseTick)
            {
                TriggerWarpPulse();
                nextPulseTick = Find.TickManager.TicksGame + Props.pulseInterval;
            }

            // 3. Temperatur Check (Kernschmelze-Logik)
            float roomTemp = parent.AmbientTemperature;
            if (roomTemp > Props.safeTemperatureMax)
            {
                instabilityCounter++;
                if (instabilityCounter > 600) 
                {
                    parent.TakeDamage(new DamageInfo(DamageDefOf.Burn, Props.damagePerTick, 0f, -1f, parent));
                    instabilityCounter = 0;
                    if (Rand.Value < 0.2f) 
                        Messages.Message("Warning: Warp Core Overheating! Coolant systems failing!", parent, MessageTypeDefOf.NegativeEvent);
                }
            }
            else
            {
                if (instabilityCounter > 0) instabilityCounter--;
            }
        }

        private void TriggerWarpPulse()
        {
            if (parent.Map == null) return;

            // Wir suchen den FleckDef, den wir im XML definiert haben
            FleckDef pulseFleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_WarpPulse");
            
            // Fallback: Falls XML fehlt, nutzen wir "PsycastAreaEffect" (ist lila, aber besser als Fehler)
            if (pulseFleck == null) pulseFleck = FleckDefOf.PsycastAreaEffect;

            // Effekt abfeuern (ähnlich wie im Obelisk Script)
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

            yield return new Command_Action
            {
                defaultLabel = "EJECT WARP CORE",
                defaultDesc = "EMERGENCY: Ejects the core to prevent a breach.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/EjectCore", false), 
                defaultIconColor = Color.red,
                action = () =>
                {
                    ejected = true;
                    Messages.Message("Warp Core ejected successfully!", MessageTypeDefOf.PositiveEvent);
                    parent.Destroy(DestroyMode.KillFinalize); 
                }
            };
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (!ejected && mode == DestroyMode.KillFinalize)
            {
                var refuelable = parent.TryGetComp<CompRefuelable>();
                if (refuelable != null && refuelable.HasFuel)
                {
                    if (previousMap != null)
                    {
                        Messages.Message("WARP CORE BREACH DETECTED!", MessageTypeDefOf.ThreatBig);
                        
                        // Explosions-Fix
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