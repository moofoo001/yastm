using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace YASTM
{
    // ---------------------------------------------------------
    // 1. DER JOB DRIVER (Die "Behandlung")
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
            // 1. Zum Gefangenen gehen
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                                   .FailOnDespawnedNullOrForbidden(TargetIndex.B);

            // 2. Gefangenen schnappen (Tragen)
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);

            // 3. Zur Maschine tragen
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 4. Gefangenen in die Maschine "legen" (Drop on cell)
            yield return new Toil
            {
                initAction = () =>
                {
                    pawn.carryTracker.TryDropCarriedThing(Machine.Position, ThingPlaceMode.Direct, out _);
                    Prisoner.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 500), JobCondition.InterruptForced);
                    Prisoner.Rotation = Rot4.South; // Blick nach vorne
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // 5. DAS VERHÖR (bzzzzzzzZZ)
            Toil sift = Toils_General.Wait(400); // Ca. 7 Sekunden
            sift.WithProgressBarToilDelay(TargetIndex.A);
            sift.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            sift.tickAction = () =>
            {
                // Visuelle Effekte (Funken/Text)
                if (pawn.IsHashIntervalTick(100))
                {
                    MoteMaker.ThrowText(Machine.DrawPos, Map, "ST_Mote_Bzzzt".Translate(), Color.red);
                    FleckMaker.ThrowMicroSparks(Prisoner.DrawPos, Map);
                }
            };
            yield return sift;

            // 6. Abschluss & Konsequenzen
            yield return new Toil
            {
                initAction = () =>
                {
                    // A. Widerstand brechen (Massiv!)
                    if (Prisoner.guest != null)
                    {
                        float reduction = Rand.Range(10f, 20f);
                        Prisoner.guest.resistance = Mathf.Max(0, Prisoner.guest.resistance - reduction);
                        Messages.Message("ST_Message_MindSiftSuccess".Translate(Prisoner.LabelShort, reduction.ToString("F1")), Prisoner, MessageTypeDefOf.PositiveEvent);
                    }

                    // B. Risiko: Hirnschaden (30% Chance)
                    if (Rand.Chance(0.30f))
                    {
                        // Hardcore: Chance auf permanente Narbe oder Dementia
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
                    
                    // C. Karriere-Punkt für den Romulaner
                    var compCareer = pawn.TryGetComp<CompCareer>();
                    compCareer?.AddCareerPoint("IntelExtracted", 1);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // 7. Zurück ins Bett bringen
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_General.Do(delegate 
            {
                // FIX: GuestStatus.Prisoner statt 'false'
                Building_Bed bed = RestUtility.FindBedFor(Prisoner, pawn, true, false, GuestStatus.Prisoner);
                
                if (bed != null)
                {
                    pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(JobDefOf.EscortPrisonerToBed, Prisoner, bed));
                }
                else
                {
                    // Einfach fallen lassen wenn kein Bett da ist
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                }
            });
        }
    }

    // ---------------------------------------------------------
    // 2. DER WORK GIVER (Die Suche)
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
                return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_Job_MindSift"), t, prisoner);
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