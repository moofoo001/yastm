using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Source.Jobs
{
    public class JobDriver_ManBridgeStation : JobDriver
    {
        // Helfer für sauberen Zugriff
        protected Thing Station => this.job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // FIX 1: Prüfen ob Station null oder zerstört ist, BEVOR wir reservieren
            if (Station == null || Station.Destroyed) return false;
            
            return this.pawn.Reserve(Station, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // FIX 2: Wenn Station beim Start schon weg ist -> sofort sanft beenden
            if (Station == null || Station.Destroyed)
            {
                // Erzeugt einen leeren Schritt, der den Job sofort beendet
                yield return new Toil 
                { 
                    initAction = () => EndJobWith(JobCondition.Incompletable) 
                };
                yield break; 
            }

            // Standard RimWorld Sicherheitscheck
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // 1. Zur Station gehen
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 2. Die Arbeit verrichten
            Toil work = new Toil();
            work.tickAction = delegate ()
            {
                Pawn actor = this.pawn;
                Thing station = actor.CurJob?.targetA.Thing;

                // FIX 3: Auch während der Arbeit prüfen (falls Konsole explodiert/abgebaut wird)
                if (station == null || station.Destroyed) 
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                
                // Pawn drehen
                actor.rotationTracker.FaceTarget(station);
                
                // XP geben
                actor.skills?.Learn(SkillDefOf.Intellectual, 0.035f);

                // Bedürfnisse prüfen (Hunger/Schlaf) -> Pause machen
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