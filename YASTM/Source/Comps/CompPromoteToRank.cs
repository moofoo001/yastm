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

        
                bool hasFromRank = usedBy.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamedSilentFail(ext.fromRankTraitDefName)) ?? false;
                if (!hasFromRank)
                {
                    Messages.Message("ST.Promotion.Error.WrongFromRank".Translate(usedBy.Named("PAWN")), usedBy, MessageTypeDefOf.RejectInput);
                    return;
                }

         
                var wc = Find.World.GetComponent<YASTM.WorldComponent_PromotionObjectives>();
                var pr = wc?.ActiveFor(usedBy, targetTrait);
                if (pr == null || !pr.Completed)
                {
                    Messages.Message("ST.Promotion.Error.ObjectivesMissing".Translate(usedBy.Named("PAWN")), usedBy, MessageTypeDefOf.RejectInput);
                    return;
                }

           
                if (ext.uniqueColonyWide)
                {
                    bool someoneHasTarget = PawnsFinder.AllMaps_FreeColonists.Any(c => c?.story?.traits?.HasTrait(targetTrait) == true);
                    if (someoneHasTarget)
                    {
                        Messages.Message("ST.Promotion.Error.Unique".Translate(targetTrait.label.CapitalizeFirst()), usedBy, MessageTypeDefOf.RejectInput);
                        return;
                    }
                }

           

           
                var oldRank = usedBy.story?.traits?.allTraits?
                    .FirstOrDefault(t => t.def?.defName != null && t.def.defName.StartsWith("ST_Rank_"));
                if (oldRank != null)
                    usedBy.story.traits.RemoveTrait(oldRank);

            
                usedBy.story?.traits?.GainTrait(new Trait(targetTrait));

           
                usedBy.GetComp<YASTM.CompStarfleetCareer>()?.MarkPromotedNow();

            
                if (pr != null) pr.rewardGiven = true;

          
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
               
                    var one = parent.SplitOff(1);
                    one.Destroy(DestroyMode.Vanish);
                }
                Messages.Message("ST.Promotion.Applied".Translate(usedBy.Named("PAWN")),
                    usedBy, MessageTypeDefOf.PositiveEvent);
            }
    }
}

