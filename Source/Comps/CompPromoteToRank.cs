using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YASTM
{
    // Properties: verweist auf unsere CompUseEffect
    public class CompProperties_PromoteToRank : CompProperties
    {
        public string targetRankTrait; // z.B. "ST_Rank_Ensign"

        public CompProperties_PromoteToRank()
        {
            compClass = typeof(CompUseEffect_PromoteToRank);
        }
    }

    // Richtige Basisklasse für "bei Benutzung": CompUseEffect
    public class CompUseEffect_PromoteToRank : CompUseEffect
    {
        public CompProperties_PromoteToRank Props => (CompProperties_PromoteToRank)props;

        public override void DoEffect(Pawn user)
        {
            if (user?.story?.traits == null) return;

            // Alte Rank-Traits entfernen
            var ranks = user.story.traits.allTraits
                .Where(t => t.def?.defName != null && t.def.defName.StartsWith("ST_Rank_"))
                .ToList();
            foreach (var tr in ranks)
                user.story.traits.RemoveTrait(tr);

            // Ziel-Rang setzen
            if (!Props.targetRankTrait.NullOrEmpty())
            {
                var tdef = DefDatabase<TraitDef>.GetNamedSilentFail(Props.targetRankTrait);
                if (tdef != null)
                {
                    user.story.traits.GainTrait(new Trait(tdef, degree: 0, forced: false));
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
