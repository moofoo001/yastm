// Source/Comps/CompPromoteToRank.cs
using System.Linq;
using RimWorld;
using Verse;

namespace YASTM
{
    public class CompProperties_PromoteToRank : CompProperties
    {
        public string targetRankTraitDefName;   // z.B. "ST_Rank_LieutenantJG"
        public string pipApparelDefName;        // z.B. "ST_RankPips_LJG"
        public bool consumeOnUse = true;        // rein informativ

        public CompProperties_PromoteToRank()
        {
            compClass = typeof(CompPromoteToRank);
        }
    }

    public class CompPromoteToRank : CompUseEffect
    {
        public CompProperties_PromoteToRank Props => (CompProperties_PromoteToRank)props;

        public override void DoEffect(Pawn usedBy)
            {
                base.DoEffect(usedBy);
                if (usedBy == null || usedBy.Dead) return;

                // Ziel-Rang & Extension ermitteln
                var targetTrait = !Props.targetRankTraitDefName.NullOrEmpty()
                    ? DefDatabase<TraitDef>.GetNamedSilentFail(Props.targetRankTraitDefName)
                    : null;

                if (targetTrait == null)
                {
                    Messages.Message("ST.Promotion.Error.NoTarget".Translate(), usedBy, MessageTypeDefOf.RejectInput);
                    return;
                }

                var ext = targetTrait.GetModExtension<YASTM.PromotionRequirementsExtension>();
                if (ext == null || ext.fromRankTraitDefName.NullOrEmpty())
                {
                    Messages.Message("ST.Promotion.Error.NoReqs".Translate(targetTrait.label.CapitalizeFirst()), usedBy, MessageTypeDefOf.RejectInput);
                    return;
                }

                // 1) Vor-Rang prüfen
                bool hasFromRank = usedBy.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamedSilentFail(ext.fromRankTraitDefName)) ?? false;
                if (!hasFromRank)
                {
                    Messages.Message("ST.Promotion.Error.WrongFromRank".Translate(usedBy.Named("PAWN")), usedBy, MessageTypeDefOf.RejectInput);
                    return;
                }

                // 2) Objectives abgeschlossen?
                var wc = Find.World.GetComponent<YASTM.WorldComponent_PromotionObjectives>();
                var pr = wc?.ActiveFor(usedBy, targetTrait);
                if (pr == null || !pr.Completed)
                {
                    Messages.Message("ST.Promotion.Error.ObjectivesMissing".Translate(usedBy.Named("PAWN")), usedBy, MessageTypeDefOf.RejectInput);
                    return;
                }

                // 3) Captain-Unique check (falls über Extension gefordert)
                if (ext.uniqueColonyWide)
                {
                    bool someoneHasTarget = PawnsFinder.AllMaps_FreeColonists.Any(c => c?.story?.traits?.HasTrait(targetTrait) == true);
                    if (someoneHasTarget)
                    {
                        Messages.Message("ST.Promotion.Error.Unique".Translate(targetTrait.label.CapitalizeFirst()), usedBy, MessageTypeDefOf.RejectInput);
                        return;
                    }
                }

                // ==== Ab hier: Beförderung durchführen ====

                // alten ST_Rank_* Trait entfernen
                var oldRank = usedBy.story?.traits?.allTraits?
                    .FirstOrDefault(t => t.def?.defName != null && t.def.defName.StartsWith("ST_Rank_"));
                if (oldRank != null)
                    usedBy.story.traits.RemoveTrait(oldRank);

                // Ziel-Rang vergeben
                usedBy.story?.traits?.GainTrait(new Trait(targetTrait));

                // Karriere-Timestamp setzen
                usedBy.GetComp<YASTM.CompStarfleetCareer>()?.MarkPromotedNow();

                // Progress-Eintrag als „rewarded“ markieren (falls vorhanden)
                if (pr != null) pr.rewardGiven = true;

                // Pip-Apparel auto-anlegen (falls eingetragen & vorhanden)
                if (!Props.pipApparelDefName.NullOrEmpty())
                {
                    var pipDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.pipApparelDefName);
                    if (pipDef != null)
                    {
                        Apparel apparelToWear = null;
                        if (usedBy.inventory != null)
                        {
                            apparelToWear = usedBy.inventory.innerContainer.OfType<Apparel>().FirstOrDefault(a => a.def == pipDef);
                            if (apparelToWear != null) usedBy.inventory.innerContainer.Remove(apparelToWear);
                        }
                        if (apparelToWear == null && parent is Apparel parentAsApparel && parentAsApparel.def == pipDef)
                            apparelToWear = parentAsApparel;

                        if (apparelToWear != null && usedBy.apparel != null)
                            usedBy.apparel.Wear(apparelToWear, dropReplacedApparel: true);
                    }
                }
                if (Props.consumeOnUse && !(parent is Apparel))
                {
                    // genau 1 Stück verbrauchen – funktioniert auch bei Stacks
                    var one = parent.SplitOff(1);
                    one.Destroy(DestroyMode.Vanish);
                }
                Messages.Message("ST.Promotion.Applied".Translate(usedBy.Named("PAWN")),
                    usedBy, MessageTypeDefOf.PositiveEvent);
            }
    }
}

