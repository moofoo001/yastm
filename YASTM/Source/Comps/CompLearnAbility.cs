using RimWorld;
using Verse;

namespace YASTM.Source.Comps
{
    public class CompProperties_LearnAbility : CompProperties_UseEffect
    {
        public AbilityDef ability;

        public CompProperties_LearnAbility()
        {
            this.compClass = typeof(CompUseEffect_LearnAbility);
        }
    }

    public class CompUseEffect_LearnAbility : CompUseEffect
    {
        public CompProperties_LearnAbility Props => (CompProperties_LearnAbility)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (Props.ability != null)
            {
                // check if pawn already has ability
                if (usedBy.abilities.GetAbility(Props.ability) != null)
                {
                    // already knows ability
                    Messages.Message($"{usedBy.LabelShort} already knows the secrets of {Props.ability.label}.", usedBy, MessageTypeDefOf.NeutralEvent, false);
                    return;
                }

                // grant ability
                usedBy.abilities.GainAbility(Props.ability);
                Messages.Message($"{usedBy.LabelShort} has mastered {Props.ability.label}.", usedBy, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}