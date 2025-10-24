using System.Linq;
using RimWorld;   
using Verse;

namespace YASTM
{
    public class IncidentWorker_GiveNextPromotionQuest : IncidentWorker
    {
        // Rang -> Quest
        private static readonly (string trait, string pip, string quest)[] Order =
        {
            ("ST_Rank_Ensign",      "ST_RankPips_Ensign",      "ST_Promotion_Ensign"),
            ("ST_Rank_LieutenantJ", "ST_RankPips_LieutenantJ", "ST_Promotion_LieutenantJ"),
            ("ST_Rank_Lieutenant",  "ST_RankPips_Lieutenant",  "ST_Promotion_Lieutenant"),
            ("ST_Rank_LtCommander","ST_RankPips_LtCommander","ST_Promotion_LtCommander"),
            ("ST_Rank_Commander",   "ST_RankPips_Commander",   "ST_Promotion_Commander"),
            ("ST_Rank_Captain",     "ST_RankPips_Captain",     "ST_Promotion_Captain")
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            var wc = Find.World.GetComponent<YASTM.WorldComponent_PromotionLtJG>();
            if (wc == null || wc.RewardGiven || wc.Active) return false; // schon aktiv/abgeschlossen
            var map = parms.target as Map;
            return map != null && GetNextQuestDefName(map) != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = parms.target as Map;
            var questDefName = GetNextQuestDefName(map);
            if (questDefName == null) return false;

            var q = DefDatabase<QuestScriptDef>.GetNamedSilentFail(questDefName);
            if (q == null) { Log.Error($"QuestScriptDef '{questDefName}' not found."); return false; }

            
            QuestUtility.GenerateQuestAndMakeAvailable(q, parms.points);

            var label = q.label ?? "Promotion available";
            Find.LetterStack.ReceiveLetter(label, label, def.letterDef ?? LetterDefOf.PositiveEvent,
                new TargetInfo(map.Center, map));
            return true;
        }

        private static string GetNextQuestDefName(Map map)
        {
            int highest = -1;
            for (int i = 0; i < Order.Length; i++)
            {
                if (ColonyHasRank(map, Order[i].trait, Order[i].pip)) highest = i;
                else break;
            }
            return (highest + 1 < Order.Length) ? Order[highest + 1].quest : null;
        }

        private static bool ColonyHasRank(Map map, string traitDefName, string pipDefName)
        {
            return map.mapPawns.FreeColonistsSpawned.Any(p =>
                (p.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamedSilentFail(traitDefName)) ?? false)
                || (p.apparel?.WornApparel?.Any(a => a?.def?.defName == pipDefName) ?? false)
            );
        }
    }
}
