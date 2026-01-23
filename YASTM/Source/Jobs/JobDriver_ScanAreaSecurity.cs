using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace YASTM
{
    public class JobDriver_ScanAreaSecurity : JobDriver
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
            
            // 2. Scannen
            var wait = Toils_General.Wait(scanTicks);
            wait.handlingFacing = true;
            wait.initAction = () => nextMoteTick = Find.TickManager.TicksGame;
            wait.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(TargetCell);
                // Mote Effekt
                int now = Find.TickManager.TicksGame;
                if (now >= nextMoteTick && pawn.Map != null)
                {
                    var fleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_KtarianHolo") ?? FleckDefOf.AirPuff;
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, fleck);
                    nextMoteTick = now + 120;
                }
            };
            yield return wait;

            // 3. Abschluss & Belohnung
            Toil finish = new Toil
            {
                initAction = delegate
                {
                    Pawn actor = this.pawn;

                    // A) Bestehende Hediff Logik (Buffs für Security)
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
                        Messages.Message("ST.Tricorder.Sec.Applied".Translate(actor.Named("PAWN")), actor, MessageTypeDefOf.PositiveEvent);
                    }
                    */
                    // B) NEUES CAREER SYSTEM (Universal)
                    var compCareer = actor.TryGetComp<CompCareer>();
                    if (compCareer != null)
                    {
                        // 1 Punkt für Security Sweep
                        compCareer.AddCareerPoint("SecuritySweep", 1);
                        MoteMaker.ThrowText(actor.DrawPos, actor.Map, "+1 Security Point", 2.0f);
                    }

                    // C) LOWER DECKS BUFF (Moral)
                    if (actor.story != null && actor.story.traits.HasTrait(DefDatabase<TraitDef>.GetNamed("ST_LowerDecker", false)))
                    {
                        // Security Leute freuen sich auch über den Erfolg
                        // (Wir nutzen denselben Thought oder "ST_SecuritySuccess" falls du ihn anlegst)
                        ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ST_ScienceSuccess");
                        if (thought != null)
                        {
                             actor.needs.mood.thoughts.memories.TryGainMemory(thought);
                        }
                    }

                    // D) Signal für Quests
                    Find.SignalManager.SendSignal(new Signal("STQ_SecuritySweepCompleted"));
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finish;
        }

        private CompTricorderSecurity GetTricorderComp(Pawn p)
        {
            var worn = p.apparel?.WornApparel;
            if (worn == null) return null;
            for (int i = 0; i < worn.Count; i++)
            {
                var comp = worn[i].TryGetComp<CompTricorderSecurity>();
                if (comp != null) return comp;
            }
            return null;
        }
    }
}