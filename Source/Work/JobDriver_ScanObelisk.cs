using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace StarTrekFactions.Work
{
    public class JobDriver_ScanObelisk : JobDriver
    {
        private Thing TargetThing => job.targetA.Thing;
        private StarTrekFactions.Comps.CompScanWork Comp
            => TargetThing?.TryGetComp<StarTrekFactions.Comps.CompScanWork>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Sicherheits-Fails
            this.FailOn(() => Comp == null);
            this.FailOn(() => Comp.Completed);
            this.FailOn(() => !Comp.PoweredSensorNearby());

            // 1.6: Wait + ProgressBar per WithProgressBarToilDelay
            int workTicks = Comp?.Props.workTicksBase ?? 6000;
            var work = Toils_General.Wait(workTicks);
            work.WithProgressBarToilDelay(TargetIndex.A);
            work.FailOn(() => Comp == null || !Comp.PoweredSensorNearby());
            yield return work;

            // Abschluss
            var finish = new Toil
            {
                initAction = () => { Comp?.OnScanFinished(pawn); },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finish;
        }
    }
}
