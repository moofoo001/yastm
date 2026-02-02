using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    [HarmonyPatch(typeof(PawnCapacitiesHandler), "GetLevel")]
    public static class Patch_VisorSight
    {
        // 
        [HarmonyPriority(Priority.Last)] 
        public static void Postfix(PawnCapacitiesHandler __instance, PawnCapacityDef capacity, ref float __result, Pawn ___pawn)
        {
            // 
            if (capacity != PawnCapacityDefOf.Sight) return;
            if (___pawn == null) return;

            // 
            bool hasUplink = false;
            var hediffs = ___pawn.health?.hediffSet?.hediffs;
            if (hediffs != null)
            {
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (hediffs[i].def.defName == "ST_Hediff_VisorUplink")
                    {
                        hasUplink = true;
                        break;
                    }
                }
            }

            if (hasUplink)
            {
                // blindness overwrite
                if (__result < 1.0f)
                {
                    __result = 1.0f;
                }
            }
        }
    }
}