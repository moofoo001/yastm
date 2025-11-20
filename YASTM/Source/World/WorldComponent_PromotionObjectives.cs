// Source/World/WorldComponent_PromotionObjectives.cs
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class PromotionProgress : IExposable
    {
        public Pawn pawn;
        public TraitDef targetRank;
        public int startedTick;

        public int scans;
        public int sweeps;
        public int diplomacies;

        public int reqScans = 1;
        public int reqSweeps = 1;
        public int reqDiplos = 1;

        public bool rewardGiven;

        public bool Completed =>
            scans >= reqScans &&
            sweeps >= reqSweeps &&
            diplomacies >= reqDiplos &&
            !rewardGiven;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Defs.Look(ref targetRank, "targetRank");
            Scribe_Values.Look(ref startedTick, "startedTick", 0);
            Scribe_Values.Look(ref scans, "scans", 0);
            Scribe_Values.Look(ref sweeps, "sweeps", 0);
            Scribe_Values.Look(ref diplomacies, "diplomacies", 0);
            Scribe_Values.Look(ref reqScans, "reqScans", 1);
            Scribe_Values.Look(ref reqSweeps, "reqSweeps", 1);
            Scribe_Values.Look(ref reqDiplos, "reqDiplos", 1);
            Scribe_Values.Look(ref rewardGiven, "rewardGiven", false);
        }
    }

    public class WorldComponent_PromotionObjectives : WorldComponent
    {
        public List<PromotionProgress> actives = new();
        public int maxConcurrent = 1;

        public WorldComponent_PromotionObjectives(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref actives, "ST_PromotionActives", LookMode.Deep);
            actives ??= new List<PromotionProgress>();
        }

        public bool AnyActive => actives.Any(e => e.pawn != null && !e.rewardGiven);
        public bool IsActiveFor(Pawn p) => actives.Any(e => e.pawn == p && !e.rewardGiven);

        public PromotionProgress ActiveFor(Pawn p, TraitDef target = null)
            => actives.FirstOrDefault(e => e.pawn == p && !e.rewardGiven && (target == null || e.targetRank == target));

        public (TraitDef target, PromotionRequirementsExtension ext) NextRankFor(Pawn p)
        {
            if (p?.story?.traits == null) return (null, null);

            var currentRank = p.story.traits.allTraits
                .Select(t => t.def)
                .FirstOrDefault(td => td?.defName != null && td.defName.StartsWith("ST_Rank_"));
            if (currentRank == null) return (null, null);

            foreach (var td in DefDatabase<TraitDef>.AllDefs)
            {
                var e = td.GetModExtension<PromotionRequirementsExtension>();
                if (e == null) continue;
                if (!string.Equals(e.fromRankTraitDefName, currentRank.defName)) continue;
                return (td, e);
            }
            return (null, null);
        }

        public bool CanStartFor(Pawn p)
        {
            if (p == null || !p.Spawned || p.Faction != Faction.OfPlayer) return false;
            if (IsActiveFor(p)) return false;

            if (actives.Count(e => e.pawn != null && !e.rewardGiven) >= (YASTM_Mod.Settings?.promotionMaxConcurrent ?? maxConcurrent))
                return false;

            var (target, ext) = NextRankFor(p);
            if (target == null || ext == null) return false;

            // unique colony-wide (z. B. Captain)
            if (ext.uniqueColonyWide)
            {
                if (PawnsFinder.AllMaps_FreeColonists.Any(col => col?.story?.traits?.HasTrait(target) == true))
                    return false;
                if (actives.Any(a => !a.rewardGiven && a.targetRank == target))
                    return false;
            }

            // min days since last promotion (+ Settings-Extra)
            int extra = YASTM_Mod.Settings?.minDaysExtraSinceLastPromotion ?? 0;
            int needed = ext.minDaysSinceLastPromotion + extra;
            if (needed > 0)
            {
                var career = p.GetComp<CompStarfleetCareer>();
                if (career == null) return false;
                if (career.DaysSinceLastPromotion < needed) return false;
            }

            // required completed quests
            if (ext.requiredCompletedQuests > 0)
            {
                var career = p.GetComp<CompStarfleetCareer>();
                if (career == null || career.completedPromotionQuests < ext.requiredCompletedQuests)
                    return false;
            }

            // goodwill check
            if (ext.goodwillMin > 0)
            {
                string fDef = ext.factionDefName ?? "ST_Starfleet";
                var targetFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == fDef);
                if (targetFaction != null)
                {
                    int gw = Faction.OfPlayer.GoodwillWith(targetFaction);
                    if (gw < ext.goodwillMin) return false;
                }
            }

            return true;
        }

        public bool StartForPawn(Pawn p)
        {
            var (target, ext) = NextRankFor(p);
            if (target == null || ext == null) return false;

            float mul = YASTM_Mod.Settings?.promotionObjectiveMultiplier ?? 1f;
            int Scale(int v) => Mathf.CeilToInt(v * mul);

            var prog = new PromotionProgress
            {
                pawn = p,
                targetRank = target,
                startedTick = Find.TickManager.TicksGame,
                scans = 0, sweeps = 0, diplomacies = 0,
                reqScans = Scale(ext.requiredMedOrScienceScans),
                reqSweeps = Scale(ext.requiredSecuritySweeps),
                reqDiplos = Scale(ext.requiredDiplomacyActions)
            };
            actives.Add(prog);

            Messages.Message("STQ.Promo.Start".Translate(p.Named("PAWN"), target.label.CapitalizeFirst()),
                p, MessageTypeDefOf.NeutralEvent);
            return true;
        }

        public void NotifyScanCompleted(Pawn actor)
        {
            var prog = ActiveFor(actor);
            if (prog == null) return;
            prog.scans++;
            TryComplete(prog);
        }

        public void NotifySweepCompleted(Pawn actor)
        {
            var prog = ActiveFor(actor);
            if (prog == null) return;
            prog.sweeps++;
            TryComplete(prog);
        }

        public void NotifyDiploCompleted(Pawn actor)
        {
            var prog = ActiveFor(actor);
            if (prog == null) return;
            prog.diplomacies++;
            TryComplete(prog);
        }

        void TryComplete(PromotionProgress prog)
        {
            if (prog == null || !prog.Completed) return;

            prog.rewardGiven = true;

            // Pip-Def ermitteln
            ThingDef pipDef = null;
            var ext = prog.targetRank.GetModExtension<PromotionRequirementsExtension>();
            if (!ext?.pipApparelDefName.NullOrEmpty() ?? false)
                pipDef = DefDatabase<ThingDef>.GetNamedSilentFail(ext.pipApparelDefName);

            if (pipDef == null)
            {
                var vis = prog.targetRank.GetModExtension<RankVisualExtension>();
                if (vis != null && !vis.pipApparelDefName.NullOrEmpty())
                    pipDef = DefDatabase<ThingDef>.GetNamedSilentFail(vis.pipApparelDefName);
            }
            if (pipDef == null)
            {
                string guess = "ST_RankPips_" + prog.targetRank.defName.Replace("ST_Rank_", "");
                pipDef = DefDatabase<ThingDef>.GetNamedSilentFail(guess);
            }

            Map map = prog.pawn?.Map ?? Find.AnyPlayerHomeMap;
            if (map != null)
            {
                IntVec3 cell = DropCellFinder.TradeDropSpot(map);
                if (pipDef != null)
                {
                    var pip = ThingMaker.MakeThing(pipDef);
                    GenPlace.TryPlaceThing(pip, cell, map, ThingPlaceMode.Near);
                }
                Messages.Message("STQ.Promo.Complete".Translate(prog.pawn.Named("PAWN"), prog.targetRank.label.CapitalizeFirst()),
                    new LookTargets(cell, map), MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("STQ.Promo.Complete".Translate(prog.pawn.Named("PAWN"), prog.targetRank.label.CapitalizeFirst()),
                    MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}

