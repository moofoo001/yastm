using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Jobs
{
    public class JobDriver_ManBridgeStation : JobDriver
    {
        // helpter to get
        protected Thing Station => this.job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // check if station is valid
            if (Station == null || Station.Destroyed) return false;
            
            return this.pawn.Reserve(Station, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (Station == null || Station.Destroyed)
            {
                yield return new Toil 
                { 
                    initAction = () => EndJobWith(JobCondition.Incompletable) 
                };
                yield break; 
            }

            // safe-guard
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // setup pathing
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // work toil
            Toil work = new Toil();
            work.tickAction = delegate ()
            {
                Pawn actor = this.pawn;
                Thing station = actor.CurJob?.targetA.Thing;

                // check station validity
                if (station == null || station.Destroyed) 
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                
                // face station
                actor.rotationTracker.FaceTarget(station);
                
                // gain skill experience
                actor.skills?.Learn(SkillDefOf.Intellectual, 0.035f);

                // auto end if needs are low
                if (actor.needs.food != null && actor.needs.food.CurLevelPercentage < 0.25f)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    return;
                }
                if (actor.needs.rest != null && actor.needs.rest.CurLevelPercentage < 0.25f)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                    return;
                }
            };

            work.defaultCompleteMode = ToilCompleteMode.Never;
            yield return work;
        }
    }
}