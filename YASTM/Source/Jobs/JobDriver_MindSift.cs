using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace YASTM
{
    // ---------------------------------------------------------
    // 1. job driver 
    // ---------------------------------------------------------
    public class JobDriver_MindSift : JobDriver
    {
        protected Thing Machine => job.GetTarget(TargetIndex.A).Thing;
        protected Pawn Prisoner => (Pawn)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Machine, job, 1, -1, null, errorOnFailed) && 
                   pawn.Reserve(Prisoner, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // prisoner valid?
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                                   .FailOnDespawnedNullOrForbidden(TargetIndex.B);

            // get prisoner
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);

            // carry to machine
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // drop prisoner at machine
            yield return new Toil
            {
                initAction = () =>
                {
                    pawn.carryTracker.TryDropCarriedThing(Machine.Position, ThingPlaceMode.Direct, out _);
                    Prisoner.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 500), JobCondition.InterruptForced);
                    Prisoner.Rotation = Rot4.South; 
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // MindSifting Prozess
            Toil sift = Toils_General.Wait(400); 
            sift.WithProgressBarToilDelay(TargetIndex.A);
            sift.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            sift.tickAction = () =>
            {
                // visuelle Effekte
                if (pawn.IsHashIntervalTick(100))
                {
                    MoteMaker.ThrowText(Machine.DrawPos, Map, "ST_Mote_Bzzzt".Translate(), Color.red);
                    FleckMaker.ThrowMicroSparks(Prisoner.DrawPos, Map);
                }
            };
            yield return sift;

            // Apply Effects
            yield return new Toil
            {
                initAction = () =>
                {
                    // reduce resistance
                    if (Prisoner.guest != null)
                    {
                        float reduction = Rand.Range(10f, 20f);
                        Prisoner.guest.resistance = Mathf.Max(0, Prisoner.guest.resistance - reduction);
                        Messages.Message("ST_Message_MindSiftSuccess".Translate(Prisoner.LabelShort, reduction.ToString("F1")), Prisoner, MessageTypeDefOf.PositiveEvent);
                    }

                    // chance for negative effects
                    if (Rand.Chance(0.30f))
                    {
                        // A. Minor psychic shock
                        if (Rand.Chance(0.5f))
                        {
                            BodyPartRecord brain = Prisoner.health.hediffSet.GetBrain();
                            if (brain != null)
                            {
                                Prisoner.TakeDamage(new DamageInfo(DamageDefOf.Burn, 5, 0, -1, pawn, brain));
                                Messages.Message("ST_Message_MindSiftDamage".Translate(Prisoner.LabelShort), Prisoner, MessageTypeDefOf.NegativeEvent);
                            }
                        }
                    }
                    
                    // B. Career points
                    var compCareer = pawn.TryGetComp<CompCareer>();
                    compCareer?.AddCareerPoint("IntelExtracted", 1);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // escort prisoner to bed or drop
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_General.Do(delegate 
            {
                // find bed
                Building_Bed bed = RestUtility.FindBedFor(Prisoner, pawn, true, false, GuestStatus.Prisoner);
                
                if (bed != null)
                {
                    // HIER IST DIE ÄNDERUNG:
                    Job escortJob = JobMaker.MakeJob(JobDefOf.EscortPrisonerToBed, Prisoner, bed);
                    escortJob.count = 1; // Auch der Folge-Job braucht das!
                    pawn.jobs.jobQueue.EnqueueFirst(escortJob);
                }
                else
                {
                    // no bed found, just drop the prisoner
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                }
            });
        }
    }

    // ---------------------------------------------------------
    // work giver
    // ---------------------------------------------------------
    public class WorkGiver_MindSift : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDef.Named("ST_Building_MindSifter"));
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!pawn.workSettings.WorkIsActive(WorkTypeDefOf.Warden)) return false;
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced)) return false;

            var power = t.TryGetComp<CompPowerTrader>();
            if (power == null || !power.PowerOn) return false;

            Pawn prisoner = FindPrisoner(pawn);
            if (prisoner == null) 
            {
                JobFailReason.Is("ST_NoResistantPrisoners".Translate());
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn prisoner = FindPrisoner(pawn);
            if (prisoner != null)
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_Job_MindSift"), t, prisoner);
                job.count = 1;
                return job;
            }
            return null;
        }

        private Pawn FindPrisoner(Pawn warden)
        {
            List<Pawn> prisoners = warden.Map.mapPawns.PrisonersOfColonySpawned;
            foreach (Pawn p in prisoners)
            {
                if (p.guest.resistance > 0f && 
                    !p.Downed && 
                    warden.CanReserve(p) && 
                    p.Awake())
                {
                    return p;
                }
            }
            return null;
        }
    }
}