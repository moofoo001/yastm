using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace YASTM
{
    public class JobDriver_PlayKtarianHeadset : JobDriver
    {
        Thing Headset => job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // wir reservieren nur uns selbst; das Headset ist am Pawn getragen
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Abbruchbedingungen
            this.FailOn(() => Headset == null || (Headset.ParentHolder != pawn.apparel));
            this.FailOn(() => pawn.Downed || pawn.InMentalState);
            this.FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking));

            // Optional: sich hinsetzen, wenn Stuhl in Reichweite (soft)
            // -> der Einfachheit halber hier ausgelassen; Pawn bleibt stehen

            var comp = Headset.TryGetComp<CompKtarianHeadset>();
            int duration = comp?.SessionTicks ?? 4000;

            // "Spielen"
            var wait = Toils_General.Wait(duration);
            wait.handlingFacing = true;
            wait.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(pawn.Position + IntVec3.North);
                // Ältere API: (Pawn, int joyGain, JoyTickFullJoyAction, factor, Building source)
                JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob, 1f, null);
            };
            wait.WithProgressBarToilDelay(TargetIndex.A);
            yield return wait;

            // Abschluss: Hediffs/Memory
            var finish = new Toil
            {
                initAction = delegate
                {
                    // Afterglow
                    var after = DefDatabase<HediffDef>.GetNamedSilentFail("ST_KtarianAfterglow");
                    if (after != null)
                    {
                        var h = pawn.health.hediffSet.GetFirstHediffOfDef(after) ?? pawn.health.AddHediff(after);
                        var disp = h.TryGetComp<HediffComp_Disappears>();
                        if (disp != null) disp.ticksToDisappear = Rand.RangeInclusive(15000, 30000);
                    }

                    // 1% Obsession + Memory
                    float chance = comp?.ObsessionChance ?? 0.01f;
                    if (Rand.Chance(chance))
                    {
                        var obs = DefDatabase<HediffDef>.GetNamedSilentFail("ST_KtarianObsession");
                        if (obs != null)
                        {
                            var oh = pawn.health.hediffSet.GetFirstHediffOfDef(obs) ?? pawn.health.AddHediff(obs);
                            var disp2 = oh.TryGetComp<HediffComp_Disappears>();
                            if (disp2 != null) disp2.ticksToDisappear = Rand.RangeInclusive(60000, 120000);

                            var craving = DefDatabase<ThoughtDef>.GetNamedSilentFail("ST_KtarianCraving");
                            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(craving);

                            Messages.Message("ST.Ktarian.Obsession".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NegativeEvent);
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finish;
        }
    }
}
