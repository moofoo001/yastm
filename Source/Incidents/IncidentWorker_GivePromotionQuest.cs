using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;   // QuestUtility
using Verse;

namespace YASTM
{
    // ModExtension: Voraussetzungen je Rang (Trait ODER Utility-Pip)
    public class ModExt_PromotionEligibility : DefModExtension
    {
        public List<string> requiredTraits;   // z.B. ["ST_Rank_Ensign"]
        public List<string> requiredApparels; // z.B. ["ST_RankPips_Ensign"]
    }

    // Bietet die Promotion-Quest nur an, wenn die Kolonie die Voraussetzung erfüllt
    public class IncidentWorker_GivePromotionQuest : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var map = parms.target as Map;
            if (map == null || !map.IsPlayerHome) return false;

            var ext = def.GetModExtension<ModExt_PromotionEligibility>();
            // Ensign (erste Stufe) hat evtl. keine Extension -> immer erlaubt
            if (ext == null) return true;

            return ColonyMeetsRequirements(map, ext);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = parms.target as Map;
            if (map == null) return false;

            var ext = def.GetModExtension<ModExt_PromotionEligibility>();
            if (ext != null && !ColonyMeetsRequirements(map, ext))
                return false;

            if (def.questScriptDef == null)
            {
                Log.Error($"[{def.defName}] questScriptDef is null.");
                return false;
            }

            // ⬇️ WICHTIG: Punkte (float) übergeben, NICHT die ganzen IncidentParms
            QuestUtility.GenerateQuestAndMakeAvailable(def.questScriptDef, parms.points);

            // Label/Text ggf. übersetzen (falls Keys)
            string label = def.letterLabel;
            string text  = def.letterText;
            if (!label.NullOrEmpty() && label.CanTranslate()) label = label.Translate();
            if (!text.NullOrEmpty()  && text.CanTranslate())  text  = text.Translate();

            // Letter sicher direkt senden (versionsunabhängig)
            var lookTarget = new TargetInfo(map.Center, map);
            Find.LetterStack.ReceiveLetter(label, text, def.letterDef ?? LetterDefOf.NeutralEvent, lookTarget);

            return true;
        }

        private static bool ColonyMeetsRequirements(Map map, ModExt_PromotionEligibility ext)
        {
            bool TraitCheck(Pawn p)
            {
                if (ext.requiredTraits == null || ext.requiredTraits.Count == 0) return true;
                var traits = p?.story?.traits;
                if (traits == null) return false;

                foreach (var tDefName in ext.requiredTraits)
                {
                    var tdef = DefDatabase<TraitDef>.GetNamedSilentFail(tDefName);
                    if (tdef != null && traits.HasTrait(tdef)) return true;
                }
                return false;
            }

            bool ApparelCheck(Pawn p)
            {
                if (ext.requiredApparels == null || ext.requiredApparels.Count == 0) return true;
                var wa = p?.apparel?.WornApparel;
                if (wa == null) return false;

                return wa.Any(a => a?.def != null && ext.requiredApparels.Contains(a.def.defName));
            }

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                if (TraitCheck(pawn) || ApparelCheck(pawn))   // Trait ODER passende Utility-Pip genügt
                    return true;

            return false;
        }
    }
}
