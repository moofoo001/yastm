using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace YASTM
{
    public class JobDriver_ScanAllyMedical : JobDriver
    {
        Pawn TargetPawn => (Pawn)job.targetA.Thing;
        private int nextMoteTick;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => TargetPawn == null || TargetPawn.Dead);
            this.FailOnAggroMentalState(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var comp = GetTricorderComp(pawn);
            int scanTicks = comp?.ScanTicks ?? 1200;

            var wait = Toils_General.Wait(scanTicks);
            wait.handlingFacing = true;
            wait.initAction = () => nextMoteTick = Find.TickManager.TicksGame;

            wait.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetPawn);


                int now = Find.TickManager.TicksGame;
                if (now >= nextMoteTick && pawn.Map != null)
                {
                    var fleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_KtarianHolo") ?? FleckDefOf.AirPuff;
                    FleckMaker.AttachedOverlay(TargetPawn, fleck, new Vector3(0f, 0f, 0.35f));
                    nextMoteTick = now + 60;
                }
            };
            wait.WithProgressBarToilDelay(TargetIndex.A);
            yield return wait;

            var finish = new Toil
            {
                initAction = delegate
                {
                    var compMed = GetTricorderComp(pawn);
                    string hedName = compMed?.HediffDefName ?? "ST_MedScan_Boost";
                    var def = DefDatabase<HediffDef>.GetNamedSilentFail(hedName);
                    if (def != null)
                    {
                        var h = TargetPawn.health.hediffSet.GetFirstHediffOfDef(def) ?? TargetPawn.health.AddHediff(def);
                        var disp = h.TryGetComp<HediffComp_Disappears>();
                        if (disp != null)
                        {
                        int durationTicks = (comp.HediffDuration > 0)
                            ? Mathf.Max(0, comp.HediffDuration)
                            : new IntRange(comp.Props.hediffMinTicks, comp.Props.hediffMaxTicks).RandomInRange;
                        disp.ticksToDisappear = durationTicks;
                        }
                        Messages.Message("ST.Tricorder.Med.Applied".Translate(TargetPawn.Named("PAWN")),
                            TargetPawn, MessageTypeDefOf.PositiveEvent);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finish;
            Find.SignalManager.SendSignal(new Signal("STQ_ScanCategoryCompleted")); 
        }

        private CompTricorderMedical GetTricorderComp(Pawn p)
        {
            var worn = p.apparel?.WornApparel;
            if (worn == null) return null;
            for (int i = 0; i < worn.Count; i++)
            {
                var comp = worn[i].TryGetComp<CompTricorderMedical>();
                if (comp != null) return comp;
            }
            return null;
        }
    }
}

