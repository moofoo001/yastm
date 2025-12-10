using RimWorld;
using Verse;

namespace YASTM.Comps
{
    /// <summary>
    /// When this apparel is worn, grants the pawn a cloaking ability.
    /// When removed, the ability is taken away again.
    /// </summary>
    public class CompProperties_CloakingDevice : CompProperties
    {
        public AbilityDef abilityDef;

        public CompProperties_CloakingDevice()
        {
            compClass = typeof(CompCloakingDevice);
        }
    }

    public class CompCloakingDevice : ThingComp
    {
        public CompProperties_CloakingDevice Props => (CompProperties_CloakingDevice)props;

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            GrantAbility(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveAbility(pawn);
        }

        private void GrantAbility(Pawn pawn)
        {
            if (pawn == null || Props.abilityDef == null)
                return;

            Pawn_AbilityTracker tracker = pawn.abilities;
            if (tracker == null)
                return;

            // Older RW versions do not have HasAbility(), use GetAbility() instead
            Ability existing = tracker.GetAbility(Props.abilityDef);
            if (existing == null)
            {
                tracker.GainAbility(Props.abilityDef);
                Log.Message("[YASTM][Cloak] Granted " + Props.abilityDef.defName + " to " + pawn.LabelShort);
            }
        }

        private void RemoveAbility(Pawn pawn)
        {
            if (pawn == null || Props.abilityDef == null)
                return;

            Pawn_AbilityTracker tracker = pawn.abilities;
            if (tracker == null)
                return;

            // Only call RemoveAbility if the pawn actually has it
            Ability existing = tracker.GetAbility(Props.abilityDef);
            if (existing != null)
            {
                tracker.RemoveAbility(Props.abilityDef);
                Log.Message("[YASTM][Cloak] Removed " + Props.abilityDef.defName + " from " + pawn.LabelShort);
            }
        }
    }
}
