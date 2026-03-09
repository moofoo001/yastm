using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace YASTM.Patches
{
    /// <summary>
/// prevents babies from being generated as officers
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new Type[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PreventBabyOfficers
    {
        [HarmonyPrefix]
        public static void Prefix(ref PawnGenerationRequest request)
        {
            // 1. check if baby
            if (request.AllowedDevelopmentalStages.HasFlag(DevelopmentalStage.Newborn) ||
                request.AllowedDevelopmentalStages.HasFlag(DevelopmentalStage.Baby))
            {
                // 2. check if officer
                if (request.KindDef != null && request.KindDef.defName.StartsWith("FederationPawn_"))
                {
                    // 3. replace with normal colonist
                    request.KindDef = PawnKindDefOf.Colonist; 
                }
            }
        }
    }
}