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
        private int nextMoteTick = -1;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Headset == null || (Headset.ParentHolder != pawn.apparel));
            this.FailOn(() => pawn.Downed || pawn.InMentalState);
            this.FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking));

            var comp = Headset.TryGetComp<CompKtarianHeadset>();
            int duration = comp?.SessionTicks ?? 4000;

            var wait = Toils_General.Wait(duration);
            wait.handlingFacing = true;
            wait.initAction = () => nextMoteTick = Find.TickManager.TicksGame;

            wait.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(pawn.Position + IntVec3.North);

                // Ältere API: (pawn, joyGain, action, factor, sourceBuilding)
                JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob, 1f, null);

                // --- HOLO-EFFEKT alle ~60 Ticks ---
                int now = Find.TickManager.TicksGame;
                if (now >= nextMoteTick && pawn.Map != null)
                {
                    // Nimm deinen blauen Fleck "ST_KtarianHolo"; falls nicht vorhanden, AirPuff als Fallback
                    var fleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_KtarianHolo") ?? FleckDefOf.AirPuff;

                    // ältere, robuste API
                    FleckMaker.AttachedOverlay(pawn, fleck, new Vector3(0f, 0f, 0.35f));
                    // Alternativ (falls AttachedOverlay fehlen sollte):
                    // FleckMaker.Static(pawn.DrawPos + new Vector3(0f, 0f, 0.35f), pawn.Map, fleck, 1.0f);

                    nextMoteTick = now + 60;
                }
            };
            wait.WithProgressBarToilDelay(TargetIndex.A);
            yield return wait;

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
                    float chance = Headset.TryGetComp<CompKtarianHeadset>()?.ObsessionChance ?? 0.01f;
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
