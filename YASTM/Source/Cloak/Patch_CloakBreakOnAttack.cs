// YASTM Patch_CloakBreakOnAttack v1.2
using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace YASTM
{
    [StaticConstructorOnStartup]
    public static class YASTM_CloakBreakInit
    {
        static YASTM_CloakBreakInit()
        {
           // Log.Message("[YASTM DEBUG] Initialized Patch_CloakBreakOnAttack v1.2");
        }
    }

    public static class CloakBreakLogic
    {
        public static void TryBreakCloak(Pawn pawn)
        {
            if (pawn == null) return;

            // 1. Romulanisches Tarnfeld removed
            var romulanCloak = pawn.health.hediffSet.GetFirstHediffOfDef(ST_HediffDefOf.ST_CloakingField);
            if (romulanCloak != null)
            {
                pawn.health.RemoveHediff(romulanCloak);
            }

            // 2. Jem'Hadar Shroud removed 
            HediffDef shroudDef = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Hediff_ShroudCloak");
            if (shroudDef != null)
            {
                var shroudCloak = pawn.health.hediffSet.GetFirstHediffOfDef(shroudDef);
                if (shroudCloak != null)
                {
                    pawn.health.RemoveHediff(shroudCloak);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Verb), "TryStartCastOn", new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public static class Patch_CloakBreak_Ranged
    {
        [HarmonyPrefix]
        public static void Prefix(Verb __instance)
        {
            CloakBreakLogic.TryBreakCloak(__instance.CasterPawn);
        }
    }

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