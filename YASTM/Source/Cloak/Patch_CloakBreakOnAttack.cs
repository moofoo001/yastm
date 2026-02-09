using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace YASTM
{
    // help method
    public static class CloakBreakLogic
    {
        public static void TryBreakCloak(Pawn pawn)
        {
            if (pawn == null) return;

            // Hat er den Hediff?
            var cloak = pawn.health.hediffSet.GetFirstHediffOfDef(ST_HediffDefOf.ST_CloakingField);
            if (cloak != null)
            {
                pawn.health.RemoveHediff(cloak);
            }
        }
    }

    // range attack and abilities
    [HarmonyPatch(typeof(Verb), "TryStartCastOn", new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public static class Patch_CloakBreak_Ranged
    {
        [HarmonyPrefix]
        public static void Prefix(Verb __instance)
        {
            CloakBreakLogic.TryBreakCloak(__instance.CasterPawn);
        }
    }

    // melee attack
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), "TryMeleeAttack")]
    public static class Patch_CloakBreak_Melee
    {
        [HarmonyPrefix]
        public static void Prefix(Pawn_MeleeVerbs __instance)
        {
            CloakBreakLogic.TryBreakCloak(__instance.Pawn);
        }
    }
}