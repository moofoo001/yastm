using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Source.Jobs
{
    public class JobDriver_ManBridgeStation : JobDriver
    {
        // Hilfseigenschaft
        protected Thing Station => this.job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Station, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // 1. Hingehen (OnCell)
            // Wir nutzen GotoCell(InteractionCell), um Pathing-Probleme mit Möbeln zu umgehen
            yield return Toils_Goto.GotoCell(Station.InteractionCell, PathEndMode.OnCell);

            // 2. Arbeiten
            Toil work = new Toil();
            work.tickAction = delegate ()
            {
                Pawn actor = this.pawn;
                
                // Sicherstellen, dass wir wirklich da sind (gegen Schubsen)
                if (actor.Position != Station.InteractionCell && actor.Position != Station.Position)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                actor.rotationTracker.FaceTarget(Station);
                actor.skills?.Learn(SkillDefOf.Intellectual, 0.035f);

                // Hunger/Schlaf Check (Soft Exit, damit sie Pause machen)
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