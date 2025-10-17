using System.Linq;
using RimWorld;
using Verse;

namespace YASTM
{
    // ✦ WICHTIG: von CompProperties_UseEffect erben!
    public class CompProperties_PromoteToRank : CompProperties_UseEffect
    {
        public string targetRankTrait; // z.B. "ST_Rank_Ensign"
        public CompProperties_PromoteToRank()
        {
            compClass = typeof(CompUseEffect_PromoteToRank);
        }
    }

    public class CompUseEffect_PromoteToRank : CompUseEffect
    {
        // Typisierte Props, damit wir targetRankTrait bequem lesen
        public CompProperties_PromoteToRank Props => (CompProperties_PromoteToRank)this.props;

        public override void DoEffect(Pawn user)
        {
            if (user?.story?.traits == null) return;

            // alle bestehenden Rank-Traits entfernen
            var ranks = user.story.traits.allTraits
                .Where(t => t.def?.defName != null && t.def.defName.StartsWith("ST_Rank_"))
                .ToList();
            foreach (var tr in ranks)
                user.story.traits.RemoveTrait(tr);

            // Zielrang setzen
            if (!Props.targetRankTrait.NullOrEmpty())
            {
                var tdef = DefDatabase<TraitDef>.GetNamedSilentFail(Props.targetRankTrait);
                if (tdef != null)
                {
                    user.story.traits.GainTrait(new Trait(tdef, 0, false));
                    Messages.Message($"{user.LabelShort} promoted to {tdef.label}.", user, MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("Promotion failed: trait def not found.", MessageTypeDefOf.RejectInput);
                }
            }
        }
    }
}
