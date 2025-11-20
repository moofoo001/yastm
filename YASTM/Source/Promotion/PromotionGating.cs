using System.Linq;
using RimWorld;
using Verse;

namespace YASTM
{
    public static class PromotionGating
    {
        public static bool MeetsRequirements(Pawn pawn, TraitDef targetRankTrait, out string failReason)
        {
            failReason = null;
            if (pawn == null || targetRankTrait == null) return false;

            var ext = targetRankTrait.GetModExtension<PromotionRequirementsExtension>();
            if (ext == null) return true; // keine Anforderungen hinterlegt

            // 1) Einzigartigkeit (z.B. Captain)
            if (ext.uniqueColonyWide)
            {
                bool someoneElse = PawnsFinder.AllMaps_FreeColonistsSpawned
                    .Any(p => p != pawn && p.story?.traits?.HasTrait(targetRankTrait) == true);
                if (someoneElse)
                {
                    failReason = "ST.Promo.Fail.Unique".Translate();
                    return false;
                }
            }

            // 2) Goodwill gegen Ziel-Fraktion
            if (!ext.factionDefName.NullOrEmpty())
            {
                var facDef = DefDatabase<FactionDef>.GetNamedSilentFail(ext.factionDefName);
                var fac = Find.FactionManager?.AllFactions?.FirstOrDefault(f => f.def == facDef);
                if (fac != null)
                {
                    // FIX: API heißt GoodwillWith(..), nicht GetGoodwillWith
                    int goodwill = Faction.OfPlayer.GoodwillWith(fac);
                    if (goodwill < ext.goodwillMin)
                    {
                        failReason = "ST.Promo.Fail.Goodwill".Translate(ext.goodwillMin, fac.Name);
                        return false;
                    }
                }
            }

            // 3) Mindestanzahl abgeschlossener Quests (fallback ohne Tag-Filter)
            if (ext.requiredCompletedQuests > 0)
            {
                int done = 0;
                var qs = Find.QuestManager.QuestsListForReading;
                for (int i = 0; i < qs.Count; i++)
                {
                    var q = qs[i];
                    if (q != null && q.State == QuestState.EndedSuccess)
                    {
                        // Hinweis: In dieser RW-Version ist q.root.tags nicht verfügbar.
                        // Wenn Tag-Filter nötig ist, bitte später alternative Heuristik ergänzen.
                        done++;
                    }
                }
                if (done < ext.requiredCompletedQuests)
                {
                    failReason = "ST.Promo.Fail.Quests".Translate(ext.requiredCompletedQuests);
                    return false;
                }
            }

            // 4) Mindestzeit seit letzter Beförderung
            if (ext.minDaysSinceLastPromotion > 0)
            {
                var tracker = WorldComponent_PromotionTracker.Get();
                int ticks = tracker?.TicksSinceLastPromotion(pawn) ?? int.MaxValue;
                int needTicks = ext.minDaysSinceLastPromotion * 60000; // 1 Tag = 60k Ticks
                if (ticks < needTicks)
                {
                    var remain = needTicks - ticks;
                    failReason = "ST.Promo.Fail.Time".Translate(remain.ToStringTicksToPeriod());
                    return false;
                }
            }

            return true;
        }
    }
}

