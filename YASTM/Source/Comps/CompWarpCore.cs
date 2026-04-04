using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;
using Verse.AI;     

namespace YASTM
{
    public class CompProperties_WarpCore : CompProperties
    {
        public float explosionRadius = 25.9f;
        public int ticksToBreach = 3600; 
        public int ejectionCountdown = 300; 
        public float pulseRadius = 4.9f; 
        public int pulseDamage = 5; 
        
        public CompProperties_WarpCore()
        {
            this.compClass = typeof(CompWarpCore);
        }
    }

    public class CompWarpCore : ThingComp
    {
        public CompProperties_WarpCore Props => (CompProperties_WarpCore)props;

        private bool breaching = false;
        private int ticksToExplode = -1;
        
        private bool ejectionActive = false;
        private int ejectionTicksLeft = -1;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref breaching, "breaching", false);
            Scribe_Values.Look(ref ticksToExplode, "ticksToExplode", -1);
            Scribe_Values.Look(ref ejectionActive, "ejectionActive", false);
            Scribe_Values.Look(ref ejectionTicksLeft, "ejectionTicksLeft", -1);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            // only show if on a map
            if (parent.Map == null) yield break;

            if (!ejectionActive)
            {
                yield return new Command_Action
                {
                    defaultLabel = "EJECT CORE",
                    defaultDesc = "Emergency Ejection! Launches the core structure to save the ship/colony.",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/EjectCore", true),
                    action = StartEjectionSequence
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "ABORT EJECTION",
                    defaultDesc = "Stop the ejection sequence.",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CeaseFire", true),
                    action = () => { ejectionActive = false; ejectionTicksLeft = -1; }
                };
            }
        }

        public void StartBreach()
        {
            if (breaching || ejectionActive) return;
            breaching = true;
            ticksToExplode = Props.ticksToBreach;
            
            if (ST_SoundDefOf.ST_Sound_RedAlert != null) 
                ST_SoundDefOf.ST_Sound_RedAlert.PlayOneShotOnCamera(parent.Map);
            
            FleckMaker.ThrowSmoke(parent.Position.ToVector3Shifted(), parent.Map, 5f);
            Messages.Message("WARP CORE BREACH IN PROGRESS!", parent, MessageTypeDefOf.ThreatBig, true);
        }

        public void Stabilize(Pawn engineer)
        {
            breaching = false;
            ticksToExplode = -1;
            Messages.Message($"{engineer.LabelShort} has stabilized the antimatter containment!", parent, MessageTypeDefOf.PositiveEvent, true);
        }

        private void StartEjectionSequence()
        {
            ejectionActive = true;
            ejectionTicksLeft = Props.ejectionCountdown;
            Messages.Message("CORE EJECTION SEQUENCE INITIATED!", parent, MessageTypeDefOf.CautionInput, false);
        }

        private void EjectCore()
        {
            Map map = parent.Map;
            IntVec3 pos = parent.Position;

            GenExplosion.DoExplosion(pos, map, 3.9f, DamageDefOf.Smoke, null);
            parent.Destroy(DestroyMode.Vanish);

            if (parent.def.Minifiable)
            {
                MinifiedThing minified = parent.MakeMinified();
                GenSpawn.Spawn(minified, pos, map);
            }

            Messages.Message("Warp Core ejected successfully.", MessageTypeDefOf.PositiveEvent);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (ejectionActive)
            {
                ejectionTicksLeft--;
                if (ejectionTicksLeft % 60 == 0)
                    MoteMaker.ThrowText(parent.DrawPos, parent.Map, $"EJECT: {ejectionTicksLeft/60}", Color.yellow);

                if (ejectionTicksLeft <= 0)
                {
                    EjectCore();
                    return; 
                }
            }

            if (breaching)
            {
                ticksToExplode--;

                if (ticksToExplode % 60 == 0)
                    MoteMaker.ThrowText(parent.DrawPos, parent.Map, $"BREACH: {ticksToExplode/60}", Color.red);

                if (ticksToExplode % 120 == 0) 
                {
                     DoRadiationPulse();
                }

                if (ticksToExplode <= 0)
                {
                    DoCatastrophicExplosion();
                }
            }

            // dynamic cooling system
            if (parent.IsHashIntervalTick(60)) 
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                
                // is core powered
                if (power != null && power.PowerOn)
                {
                    bool isCooled = false;
                    CompAffectedByFacilities facilities = parent.GetComp<CompAffectedByFacilities>();

                    // is cooler installed
                    if (facilities != null)
                    {
                        foreach (Thing facility in facilities.LinkedFacilitiesListForReading)
                        {
                            if (facility.def.defName == "WarpCoreCooling")
                            {
                                CompPowerTrader facPower = facility.TryGetComp<CompPowerTrader>();
                                if (facPower != null && facPower.PowerOn)
                                {
                                    isCooled = true;
                                    break;
                                }
                            }
                        }
                    }

                    // no cooler nor power 
                    if (!isCooled)
                    {
                        // massiv heat
                        GenTemperature.PushHeat(parent.Position, parent.Map, 200f);
                    }
                    else
                    {
                        // with cooler installed
                        GenTemperature.PushHeat(parent.Position, parent.Map, 10f);
                    }
                }
            }
        }

        private void DoRadiationPulse()
        {
            FleckMaker.ThrowMicroSparks(parent.Position.ToVector3Shifted(), parent.Map);
            
            // IReadOnlyList INSTEAD OF List
            IReadOnlyList<Pawn> victims = parent.Map.mapPawns.AllPawnsSpawned;
            foreach (Pawn p in victims)
            {
                if (p.Position.InHorDistOf(parent.Position, Props.pulseRadius))
                {
                    p.TakeDamage(new DamageInfo(DamageDefOf.Burn, Props.pulseDamage, 0f, -1f, parent));
                }
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (mode == DestroyMode.KillFinalize)
            {
                 DoCatastrophicExplosion(previousMap);
            }
        }

        private void DoCatastrophicExplosion(Map map = null)
        {
            if (map == null) map = parent.Map;
            if (map == null) return;

            var refuelable = parent.GetComp<CompRefuelable>();
            float fuelFactor = (refuelable != null && refuelable.HasFuel) ? 1.0f : 0.1f; 

            if (fuelFactor < 0.5f) return; 

            breaching = false; 
            
            GenExplosion.DoExplosion(
                parent.Position, 
                map, 
                Props.explosionRadius * fuelFactor, 
                DamageDefOf.Bomb, 
                parent, 
                500, 
                -1f, 
                null, 
                null, 
                null, 
                null, 
                ThingDefOf.Filth_Fuel, 
                1.0f
            );
            
            if (!parent.Destroyed) parent.Destroy(DestroyMode.KillFinalize);
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (breaching)
            {
                yield return new FloatMenuOption("Stabilize Warp Core (Critical!)", () =>
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Repair, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    Stabilize(selPawn); 
                });
            }
        }
    }
}