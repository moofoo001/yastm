using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM
{
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
            // 1. Geh zu Würmern
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
            
            // 2. Heb Würmer auf
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true);
            
            // 3. Geh zum Fass
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch); // Touch, damit er davor steht
            
            // 4. Einfüllen & STAMPFEN (Das Ritual!)
            Toil stomp = Toils_General.Wait(300); // 5 Sekunden stampfen
            stomp.WithProgressBarToilDelay(TargetIndex.A);
            stomp.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            stomp.initAction = () =>
            {
                // Optional: Sound abspielen (z.B. Squishy Sound)
            };
            stomp.tickAction = () =>
            {
                // Visuelles Stampfen (Pawn hüpft leicht)
                if (pawn.IsHashIntervalTick(10))
                {
                    // Kleiner visueller Effekt oder Jitter könnte hier rein
                    FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 0.5f);
                }
            };
            yield return stomp;

            // 5. Abschluss
            yield return new Toil
            {
                initAction = () =>
                {
                    var comp = Vat.TryGetComp<CompBloodwineVat>();
                    int amountAdded = Worms.stackCount;
                    if (amountAdded > job.count) amountAdded = job.count;
                    
                    comp.AddWorms(amountAdded);
                    Worms.SplitOff(amountAdded).Destroy();
                    
                    // Karriere-Punkt für Klingonen? ;)
                    // pawn.TryGetComp<CompCareer>()?.AddCareerPoint("WormsStomped", amountAdded);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
    
    // Kleiner Extra WorkGiver, um das fertige Zeug rauszuholen
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
                    Thing wine = comp.TakeOutWine();
                    GenPlace.TryPlaceThing(wine, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}