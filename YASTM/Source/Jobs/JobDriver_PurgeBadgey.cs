using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class JobDriver_PurgeBadgey : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. Go to the console
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            
            // 2. Hack the console (wait 2 seconds with progress bar)
            yield return Toils_General.Wait(120).WithProgressBarToilDelay(TargetIndex.A);
            
            // 3. Execute
            yield return new Toil
            {
                initAction = () =>
                {
                    var comp = (TargetA.Thing).TryGetComp<CompBadgeyShutdown>();
                    comp?.DoPurge(pawn); 
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}