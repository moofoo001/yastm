using HarmonyLib;
using RimWorld;
using Verse;

namespace YASTM.HarmonyPatches
{
    /// <summary>
    /// Harmony patch to remove specific needs (Food, Rest, Joy, Comfort) for the Jem'Hadar race.
    /// This reflects their genetic engineering by the Founders.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Patch_ST_JemHadarNeeds
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result, Pawn ___pawn)
        {
            // If the game already decided this pawn shouldn't have the need, we don't need to do anything
            if (!__result) return;

            // Safety null-check for the pawn, its def, and the need
            if (___pawn == null || ___pawn.def == null || nd == null) return;

            // Check if the pawn is our Jem'Hadar via Biotech Genes/Xenotype
            if (___pawn.genes != null && ___pawn.genes.Xenotype != null)
            {
                // Matching the exact defName from ST_XenotypeDefs_Dominion.xml
                if (___pawn.genes.Xenotype.defName == "Ectos_JemHadar") 
                {
                    // Comparing defNames as strings prevents missing DefOf compilation errors.
                    // "Joy" is Recreation, "Rest" is Sleep.
                    if (nd.defName == "Food" || 
                        nd.defName == "Rest" || 
                        nd.defName == "Joy" || 
                        nd.defName == "Beauty" ||
                        nd.defName == "Comfort")
                    {
                        __result = false; // Need is completely removed
                    }
                }
            }
        }
    }
}