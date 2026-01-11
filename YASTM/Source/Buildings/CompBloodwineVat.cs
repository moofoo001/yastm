using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace YASTM
{
    // ----------------------------------------------------------------------
    // 1. DAS GEBÄUDE (COMP)
    // ----------------------------------------------------------------------
    public class CompBloodwineVat : ThingComp
    {
        public int wormCount;
        public float fermentationProgress;
        
        public const int MaxCapacity = 25;
        public const float DaysToFerment = 6f; 
        public const float MinIdealTemp = 10f; 
        public const float MaxIdealTemp = 40f; 

        public bool Empty => wormCount <= 0;
        public bool Full => wormCount >= MaxCapacity;
        public bool Fermented => fermentationProgress >= 1f;

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!Empty && !Fermented)
            {
                float temp = parent.AmbientTemperature;
                if (temp > MinIdealTemp && temp < MaxIdealTemp)
                {
                    fermentationProgress += 1f / (DaysToFerment * 240f); 
                }
            }
        }

        public void AddWorms(int count)
        {
            wormCount += count;
            if (wormCount > MaxCapacity) wormCount = MaxCapacity;
            fermentationProgress = 0f; 
        }

        public Thing TakeOutWine()
        {
            if (!Fermented) return null;
            Thing wine = ThingMaker.MakeThing(ThingDef.Named("ST_Bloodwine"));
            wine.stackCount = wormCount;
            wormCount = 0;
            fermentationProgress = 0f;
            return wine;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            if (!Empty)
            {
                if (Fermented)
                    sb.AppendLine("ST_VatReady".Translate(wormCount));
                else
                    sb.AppendLine("ST_VatFermenting".Translate(fermentationProgress.ToStringPercent(), wormCount));
                
                if (parent.AmbientTemperature < MinIdealTemp)
                    sb.AppendLine("ST_VatTooCold".Translate());
                else if (parent.AmbientTemperature > MaxIdealTemp)
                    sb.AppendLine("ST_VatTooHot".Translate());
            }
            else
            {
                sb.AppendLine("ST_VatEmpty".Translate());
            }
            return sb.ToString().TrimEnd();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref wormCount, "wormCount", 0);
            Scribe_Values.Look(ref fermentationProgress, "progress", 0f);
        }
    }

    public class CompProperties_BloodwineVat : CompProperties
    {
        public CompProperties_BloodwineVat()
        {
            this.compClass = typeof(CompBloodwineVat);
        }
    }

    // ----------------------------------------------------------------------
    // 2. DER JOB DRIVER (ANIMATION)
    // ----------------------------------------------------------------------
    public class JobDriver_FillBloodwineVat : JobDriver
    {
        protected Thing Vat => job.GetTarget(TargetIndex.A).Thing;
        protected Thing Worms => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Vat, job, 1, -1, null, errorOnFailed) && 
                   pawn.Reserve(Worms, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            
            Toil stomp = Toils_General.Wait(300);
            stomp.WithProgressBarToilDelay(TargetIndex.A);
            stomp.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return stomp;

            yield return new Toil
            {
                initAction = () =>
                {
                    var comp = Vat.TryGetComp<CompBloodwineVat>();
                    if (comp != null && Worms != null)
                    {
                        int amountAdded = Worms.stackCount;
                        if (amountAdded > job.count) amountAdded = job.count;
                        comp.AddWorms(amountAdded);
                        Worms.SplitOff(amountAdded).Destroy();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }

    public class JobDriver_TakeBloodwine : JobDriver
    {
        protected Thing Vat => job.GetTarget(TargetIndex.A).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Vat, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(60).WithProgressBarToilDelay(TargetIndex.A);
            yield return new Toil
            {
                initAction = () =>
                {
                    var comp = Vat.TryGetComp<CompBloodwineVat>();
                    if (comp != null)
                    {
                        Thing wine = comp.TakeOutWine();
                        if (wine != null) GenPlace.TryPlaceThing(wine, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }

    // ----------------------------------------------------------------------
    // 3. DER WORK GIVER (KI)
    // ----------------------------------------------------------------------
    public class WorkGiver_FillBloodwineVat : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDef.Named("ST_Building_BloodwineVat"));
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building)) return false; 
            
            var comp = t.TryGetComp<CompBloodwineVat>();
            if (comp == null || comp.Fermented || comp.Full) return false;

            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced)) return false;

            if (FindWorms(pawn) == null) 
            {
                JobFailReason.Is("ST_NoWorms".Translate());
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompBloodwineVat>();
            Thing worms = FindWorms(pawn);
            
            if (worms != null)
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_Job_FillBloodwineVat"), t, worms);
                job.count = CompBloodwineVat.MaxCapacity - comp.wormCount;
                return job;
            }
            return null;
        }

        private Thing FindWorms(Pawn pawn)
        {
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, 
                ThingRequest.ForDef(ThingDef.Named("ST_RawSerpentWorms")), 
                PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, 
                x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
        }
    }

    public class WorkGiver_TakeBloodwine : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDef.Named("ST_Building_BloodwineVat"));
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompBloodwineVat>();
            return comp != null && comp.Fermented && !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_Job_TakeBloodwine"), t);
        }
    }
}