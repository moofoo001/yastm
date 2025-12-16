using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace YASTM
{
    public class JobDriver_ScanAreaScience : JobDriver
    {
        IntVec3 TargetCell => job.targetA.Cell;
        private int nextMoteTick;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !TargetCell.InBounds(pawn.Map));
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            var comp = GetTricorderComp(pawn);
            int scanTicks = comp?.ScanTicks ?? 900;
            float radius  = comp?.ScanRange ?? 18f;

            var wait = Toils_General.Wait(scanTicks);
            wait.handlingFacing = true;
            wait.initAction = () => nextMoteTick = Find.TickManager.TicksGame;
            wait.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(TargetCell);
                // kleiner Scan-Ring
                int now = Find.TickManager.TicksGame;
                if (now >= nextMoteTick && pawn.Map != null)
                {
                    var fleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_KtarianHolo") ?? FleckDefOf.AirPuff;
                    FleckMaker.Static(TargetCell.ToVector3Shifted(), pawn.Map, fleck, 1.1f);
                    nextMoteTick = now + 60;
                }
            };
            wait.WithProgressBarToilDelay(TargetIndex.A);
            yield return wait;

            var finish = new Toil
            {
                initAction = delegate
                {
                    var map = pawn.Map;
                    if (map != null)
                    {

                        foreach (var cell in GenRadial.RadialCellsAround(TargetCell, radius, true))
                        {
                            if (!cell.InBounds(map)) continue;
                            var things = map.thingGrid.ThingsListAtFast(cell);
                            for (int i = 0; i < things.Count; i++)
                            {
                                var t = things[i];
                                if (t?.def?.building != null && (t.def.building.isResourceRock || t.def.building.isNaturalRock))
                                {

                                    var fleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_KtarianHolo") ?? FleckDefOf.AirPuff;
                                    FleckMaker.Static(t.DrawPos + new Vector3(0f, 0f, 0.35f), map, fleck, 1.2f);
                                }
                            }
                        }
                    }


                    var compMed = GetTricorderComp(pawn);
                    string hedName = compMed?.HediffDefName ?? "ST_ScienceInsight";
                    var def = DefDatabase<HediffDef>.GetNamedSilentFail(hedName);
                    if (def != null)
                    {
                        var h = pawn.health.hediffSet.GetFirstHediffOfDef(def) ?? pawn.health.AddHediff(def);
                        var disp = h.TryGetComp<HediffComp_Disappears>();
                        if (disp != null)
                        {
                            var dur = compMed != null ? compMed.HediffDuration : new IntRange(30000, 45000);
                            disp.ticksToDisappear = Rand.RangeInclusive(dur.min, dur.max);
                        }
                        Messages.Message("ST.Tricorder.Sci.Applied".Translate(pawn.Named("PAWN")),
                            pawn, MessageTypeDefOf.PositiveEvent);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finish;
            Find.SignalManager.SendSignal(new Signal("STQ_ScanCategoryCompleted")); 
        }

        private CompTricorderScience GetTricorderComp(Pawn p)
        {
            var worn = p.apparel?.WornApparel;
            if (worn == null) return null;
            for (int i = 0; i < worn.Count; i++)
            {
                var comp = worn[i].TryGetComp<CompTricorderScience>();
                if (comp != null) return comp;
            }
            return null;
        }
    }
}

