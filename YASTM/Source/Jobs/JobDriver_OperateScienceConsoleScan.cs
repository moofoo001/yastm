using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using YASTM;
using YASTM.MapSystems;


namespace YASTM.Jobs
{

    public class JobDriver_OperateScienceConsoleScan : JobDriver
    {
        private const TargetIndex ConsoleInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(ConsoleInd);
            this.FailOnForbidden(ConsoleInd);

            yield return Toils_Goto.GotoThing(ConsoleInd, PathEndMode.InteractionCell);

        
            var work = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 600
            };

           
            work.WithProgressBar(ConsoleInd, () => work.actor?.jobs?.curDriver?.ticksLeftThisToil is int t ? 1f - (t / 600f) : 0f);

            work.initAction = () =>
            {
            
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Initiating subspace scan�");
            };

            work.AddFinishAction(() =>
            {
                var map = pawn.Map;
                var flow = map?.GetComponent<MapComponent_ObeliskFlow>();
            
                bool atA = true; 
                flow.RegisterScan(atA);
            });

            yield return work;
        }
    }
}

