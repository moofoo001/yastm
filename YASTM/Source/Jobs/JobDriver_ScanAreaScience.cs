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
            
            // 1. Zum Ziel gehen
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            // Tricorder Werte holen
            var comp = GetTricorderComp(pawn);
            int scanTicks = comp?.ScanTicks ?? 900;
            // float radius  = comp?.ScanRange ?? 18f; // Wird aktuell nicht genutzt, aber gut vorbereitet

            // 2. Scannen (Warten mit Effekt)
            var wait = Toils_General.Wait(scanTicks);
            wait.handlingFacing = true;
            wait.initAction = () => nextMoteTick = Find.TickManager.TicksGame;
            wait.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(TargetCell);
                
                // Visueller Effekt (Holo/Scan) alle paar Ticks
                int now = Find.TickManager.TicksGame;
                if (now >= nextMoteTick && pawn.Map != null)
                {
                    var fleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_KtarianHolo") ?? FleckDefOf.AirPuff;
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, fleck);
                    nextMoteTick = now + 120; // alle 2 Sekunden
                }
            };
            yield return wait;

            // 3. Abschluss & Belohnung
            Toil finish = new Toil
            {
                initAction = delegate
                {
                    Pawn actor = this.pawn;

                    // A) Bestehende Buff-Logik (Hediffs verteilen)
                    // Science Tricorder kann manchmal Buffs geben oder heilen (je nach deiner Def)
                    /*if (comp != null && comp.Props.hediffToApply != null)
                    {
                        HediffDef def = comp.Props.hediffToApply;
                        var h = actor.health.hediffSet.GetFirstHediffOfDef(def) ?? actor.health.AddHediff(def);
                        var disp = h.TryGetComp<HediffComp_Disappears>();
                        if (disp != null)
                        {
                            var dur = comp.HediffDuration;
                            disp.ticksToDisappear = Rand.RangeInclusive(dur.min, dur.max);
                        }
                        Messages.Message("ST.Tricorder.Sci.Applied".Translate(actor.Named("PAWN")), actor, MessageTypeDefOf.PositiveEvent);
                    }
                    */
                    // B) NEUES CAREER SYSTEM (Universal)
                    var compCareer = actor.TryGetComp<CompCareer>();
                    if (compCareer != null)
                    {
                        // 2 Punkte für Science Scans
                        compCareer.AddCareerPoint("ScienceScan", 2); 
                        MoteMaker.ThrowText(actor.DrawPos, actor.Map, "+2 Science Points", 2.0f);
                    }

                    // C) LOWER DECKS BUFF (Moral)
                    // Wenn der Pawn ein "Lower Decker" ist, freut er sich
                    if (actor.story != null && actor.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("ST_LowerDecker", false)))
                    {
                        // Versuche den Gedanken zu geben (falls Def existiert), sonst ignorieren
                        ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ST_ScienceSuccess");
                        if (thought != null)
                        {
                             actor.needs.mood.thoughts.memories.TryGainMemory(thought);
                        }
                    }

                    // D) Signal für Quests
                    Find.SignalManager.SendSignal(new Signal("STQ_ScienceSweepCompleted")); 
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finish;
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