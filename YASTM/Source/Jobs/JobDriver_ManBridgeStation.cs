using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Source.Jobs
{
    public class JobDriver_ManBridgeStation : JobDriver
    {
        // Zugriff auf das Ziel (die Konsole)
        protected Thing Station => this.job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Hier war es korrekt ("this.pawn")
            return this.pawn.Reserve(this.Station, this.job, 1, -1, null, errorOnFailed);
        }

        private bool CanWork(Pawn pawn, Thing station)
        {
            CompPowerTrader compPowerTrader = station.TryGetComp<CompPowerTrader>();
            return (compPowerTrader == null || compPowerTrader.PowerOn) && !station.IsBrokenDown();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            
            // KORREKTUR: "this.pawn" statt "this.Actor"
            this.FailOn(() => !this.CanWork(this.pawn, this.Station));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            Toil work = new Toil();
            work.tickAction = delegate ()
            {
                // KORREKTUR: "this.pawn" statt "this.Actor"
                Pawn actor = this.pawn;
                
                // Skill gain (Lernen während der Arbeit)
                actor.skills.Learn(SkillDefOf.Intellectual, 0.035f, false);
                
                // Hier können Sie bei Bedarf weitere Logik einfügen (z.B. Scannen)
            };

            // --- ANTI-STUCK LOGIK ---
            // Breche ab, wenn hungrig (unter 25%)
            work.FailOn(() => pawn.needs.food != null && pawn.needs.food.CurLevelPercentage < 0.25f);
            // Breche ab, wenn müde (unter 25%)
            work.FailOn(() => pawn.needs.rest != null && pawn.needs.rest.CurLevelPercentage < 0.25f);
            // Breche ab, wenn Recreation extrem niedrig (unter 5%)
            work.FailOn(() => pawn.needs.joy != null && pawn.needs.joy.CurLevelPercentage < 0.05f);
            // ------------------------

            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.WithEffect(EffecterDefOf.Research, TargetIndex.A);
            work.activeSkill = (() => SkillDefOf.Intellectual);
            yield return work;
        }
    }
}