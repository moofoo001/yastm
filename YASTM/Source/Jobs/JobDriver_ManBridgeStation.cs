using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace YASTM.Source.Jobs
{
    public class JobDriver_ManBridgeStation : JobDriver
    {
        // Konstanten
        private const int TickInterval = 2000; // Wie lange ein "Arbeitsblock" dauert

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserviere den Stuhl/die Konsole
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fehlerbedingungen
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);

            // 1. Gehe zur Station
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 2. Arbeite an der Station
            Toil work = new Toil();
            work.tickAction = delegate
            {
                Pawn actor = this.pawn;
                Building building = (Building)actor.CurJob.targetA.Thing;
                
                // Falls das Gebäude CompMannable hat (z.B. für Turret-Logik), feuern wir das manuell
                CompMannable mannable = building.GetComp<CompMannable>();
                if (mannable != null)
                {
                    mannable.ManForATick(actor);
                }

                // Skill-Gain (Intellektuell oder Sozial für Captains?)
                // Wir nehmen Intellectual als Standard für Brückenoffiziere
                actor.skills.Learn(SkillDefOf.Intellectual, 0.03f);
                
                // Ein bisschen Joy, damit sie nicht durchdrehen, wenn sie den ganzen Tag sitzen
                actor.needs.joy.GainJoy(0.0001f, JoyKindDefOf.Meditative);
            };
            
            // Standard: Endlos bis Zeitplan sich ändert oder Bedürfnisse fallen
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            
            // Optional: Visueller Effekt (Sprechen, Tippen)
            work.WithEffect(EffecterDefOf.Research, TargetIndex.A);

            yield return work;
        }
    }
}