using HarmonyLib;
using RimWorld;
using Verse;

namespace YASTM.Cloak
{
    /// <summary>
    /// Shared helper for cloak-related patches.
    /// </summary>
    public static class CloakUtility
    {
        private static HediffDef psychicInvisibilityDef;

        public static HediffDef CloakHediff
        {
            get
            {
                if (psychicInvisibilityDef == null)
                {
                    psychicInvisibilityDef =
                        DefDatabase<HediffDef>.GetNamedSilentFail("PsychicInvisibility");
                }

                return psychicInvisibilityDef;
            }
        }

        public static void TryBreakCloak(Pawn caster, LocalTargetInfo target, string attackType)
        {
            var hediffDef = CloakHediff;
            if (hediffDef == null || caster == null || caster.health == null)
                return;

            // is active cloak present?
            Hediff cloak = caster.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (cloak == null)
                return;

            Thing targetThing = target.Thing;
            if (targetThing == null)
                return;

            // no self-attack
            if (targetThing == caster)
                return;

            // remove Cloak
            caster.health.RemoveHediff(cloak);

            string targetName = (targetThing as Pawn)?.LabelShort ?? targetThing.LabelCap;
            Log.Message("[YASTM][Cloak] Cloak removed from "
                        + caster.LabelShort + " due to " + attackType
                        + " against " + targetName);
        }
    }

    /// <summary>
    /// Removes the cloaking hediff when a pawn with an active cloak
    /// performs a successful hostile ranged attack.
    /// </summary>
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Patch_CloakBreakOnRangedAttack
    {
        public static void Postfix(Verb_LaunchProjectile __instance, ref bool __result)
        {

            if (!__result)
                return;

            Pawn caster = __instance.CasterPawn;
            if (caster == null)
                return;

            CloakUtility.TryBreakCloak(caster, __instance.CurrentTarget, "ranged attack");
        }
    }

    /// <summary>
    /// Removes the cloaking hediff when a pawn with an active cloak
    /// performs a successful melee attack.
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_CloakBreakOnMeleeAttack
    {
        public static void Postfix(Verb_MeleeAttack __instance, ref bool __result)
        {

            if (!__result)
                return;

            Pawn caster = __instance.CasterPawn;
            if (caster == null)
                return;

            CloakUtility.TryBreakCloak(caster, __instance.CurrentTarget, "melee attack");
        }
    }
}
