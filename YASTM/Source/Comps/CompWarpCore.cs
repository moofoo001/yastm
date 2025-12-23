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
        
        // Timer for warp pulse effect
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
            
            // activity check
            if (!IsActive()) return;

            // visual warp pulse effect
            if (Find.TickManager.TicksGame >= nextPulseTick)
            {
                TriggerWarpPulse();
                nextPulseTick = Find.TickManager.TicksGame + Props.pulseInterval;
            }

            // overheating logic
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


            FleckDef pulseFleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_WarpPulse");
            

            if (pulseFleck == null) pulseFleck = FleckDefOf.PsycastAreaEffect;

            //  Create the fleck effect
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
                defaultIconColor = Color.red : Color.white,
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