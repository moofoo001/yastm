using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound; 

namespace YASTM
{
    public class JobDriver_UseTurbolift : JobDriver
    {
        private Building TurboliftStart => TargetA.Thing as Building;
        private Building TurboliftEnd => TargetB.Thing as Building;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserviere Start UND Ziel, damit kein anderer Pawn gleichzeitig reinspringt
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed) && 
                   pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.B);

            // 1. Gehe zum Lift
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 2. Warte kurz (Türanimation / Einsteigen)
            yield return Toils_General.Wait(60); // Erhöht auf 1 Sekunde (realistischer)

            // 3. TRANSFER
            yield return new Toil
            {
                initAction = () =>
                {
                    Building startLift = TurboliftStart;
                    Building endLift = TurboliftEnd;

                    if (endLift == null || !endLift.Spawned)
                    {
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    // --- LIMITER LOGIK ---
                    // Cooldown auf BEIDE Lifte anwenden!
                    startLift.GetComp<CompTurbolift>()?.StartCooldown();
                    endLift.GetComp<CompTurbolift>()?.StartCooldown();
                    // ---------------------

                    if (startLift != null && startLift.def.soundInteract != null)
                        startLift.def.soundInteract.PlayOneShot(new TargetInfo(startLift.Position, Map));

                    IntVec3 dest = endLift.InteractionCell;
                    if (!dest.Walkable(endLift.Map)) dest = endLift.Position;

                    pawn.Position = dest;
                    pawn.Notify_Teleported(true, false);
                    
                    if (endLift.def.soundInteract != null)
                        endLift.def.soundInteract.PlayOneShot(new TargetInfo(endLift.Position, endLift.Map));
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}