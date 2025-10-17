using System.Linq;
using RimWorld;
using Verse;

namespace YASTM
{
    // Properties: richtige Basisklasse für "Use"-Effekte + optionales Auto-Equip
    public class CompProperties_PromoteToRank : CompProperties_UseEffect
    {
        public string targetRankTrait;          // z.B. "ST_Rank_Ensign"
        public bool autoEquipUtilityPip = true; // Utility-Pip nach Beförderung automatisch anlegen

        public CompProperties_PromoteToRank()
        {
            compClass = typeof(CompUseEffect_PromoteToRank);
        }
    }

    // UseEffect: entfernt alte Rank-Traits, setzt neuen, legt (optional) Utility-Pip an
    public class CompUseEffect_PromoteToRank : CompUseEffect
    {
        public CompProperties_PromoteToRank Props => (CompProperties_PromoteToRank)props;

        public override void DoEffect(Pawn user)
        {
            if (user?.story?.traits == null) return;

            // 1) Alle vorhandenen ST_Rank_* Traits entfernen
            var ranks = user.story.traits.allTraits
                .Where(t => t.def?.defName != null && t.def.defName.StartsWith("ST_Rank_"))
                .ToList();
            foreach (var tr in ranks)
                user.story.traits.RemoveTrait(tr);

            // 2) Ziel-Rang setzen
            TraitDef tdef = null;
            if (!Props.targetRankTrait.NullOrEmpty())
                tdef = DefDatabase<TraitDef>.GetNamedSilentFail(Props.targetRankTrait);

            if (tdef != null)
            {
                user.story.traits.GainTrait(new Trait(tdef, degree: 0, forced: false));

                // Übersetzter Promotion-Text (fällt sauber auf Englisch zurück, falls Key fehlt)
                const string key = "ST.Rank.Promoted";
                if (key.CanTranslate())
                    Messages.Message(key.Translate(user.LabelShort, tdef.label), user, MessageTypeDefOf.PositiveEvent);
                else
                    Messages.Message($"{user.LabelShort} promoted to {tdef.label}.", user, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Promotion failed: trait def not found.", MessageTypeDefOf.RejectInput);
                return;
            }

            // 3) (Optional) Utility-Pip automatisch anlegen (Utility-Slot/Belt), unsichtbar am Pawn
            if (!Props.autoEquipUtilityPip || user.apparel == null) return;

            var pipDefName = RankTraitToPipDefName(Props.targetRankTrait);
            if (pipDefName.NullOrEmpty()) return;

            var pipDef = DefDatabase<ThingDef>.GetNamedSilentFail(pipDefName);
            if (pipDef == null || pipDef.apparel == null) return;

            // Pawn kann das Item tragen?
            if (!ApparelUtility.HasPartsToWear(user, pipDef)) return;

            var newPip = ThingMaker.MakeThing(pipDef) as Apparel;
            if (newPip == null) return;

            // Falls bereits etwas im Utility/Belt-Layer getragen wird, droppen wir es neben den Pawn
            var wornUtility = user.apparel.WornApparel
                .FirstOrDefault(a => a.def?.apparel?.layers != null && a.def.apparel.layers.Contains(ApparelLayerDefOf.Belt));
            if (wornUtility != null)
            {
                user.apparel.Remove(wornUtility);
                GenPlace.TryPlaceThing(wornUtility, user.PositionHeld, user.MapHeld, ThingPlaceMode.Near);
            }

            // Neues Utility-Pip anlegen (dropReplacedApparel zur Sicherheit true)
            user.apparel.Wear(newPip, dropReplacedApparel: true);
            parent.Destroy(DestroyMode.Vanish);
        }
            
        // Mapping Rank-Trait -> Utility-Pip-ThingDef
        static string RankTraitToPipDefName(string rankTraitDefName)
        {
            switch (rankTraitDefName)
            {
                case "ST_Rank_Ensign":       return "ST_RankPips_Ensign";
                case "ST_Rank_LieutenantJG": return "ST_RankPips_LJG";
                case "ST_Rank_Lieutenant":   return "ST_RankPips_Lieutenant";
                case "ST_Rank_LtCommander":  return "ST_RankPips_LtCommander";
                case "ST_Rank_Commander":    return "ST_RankPips_Commander";
                case "ST_Rank_Captain":      return "ST_RankPips_Captain";
                default: return null;
            }
        }
    }
}
