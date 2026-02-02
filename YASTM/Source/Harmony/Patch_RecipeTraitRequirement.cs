using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace YASTM
{
    // Patch to enforce trait requirements for recipes
    [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing")]
    public static class Patch_RecipeTraitRequirement
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Thing thing, bool forced, ref Job __result)
        {
            // if the thing is not a bill giver, we skip
            if (thing is not IBillGiver billGiver) return true;

            // check all bills on this bill giver
            foreach (Bill bill in billGiver.BillStack)
            {
                // Get our custom extension
                var extension = bill.recipe.GetModExtension<ST_RecipeExtension>();
                
                if (extension != null && extension.mustHaveTrait)
                {
                    // check if the pawn has the required trait
                    Trait currentTrait = pawn.story?.traits?.GetTrait(extension.requiredTrait);

                    if (currentTrait == null)
                    {

                        continue; 
                    }

                    // check for required degree if specified
                    if (extension.requiredDegree != -999)
                    {
                        if (currentTrait.Degree != extension.requiredDegree)
                        {
                            // pawn does not meet the degree requirement, skip this bill
                            continue;
                        }
                    }
                }
            }
            return true;
        }
    }

    // ALTERNATIVE & BESSERE METHODE: Patchen der "ShouldDoNow" Logik direkt am Bill
    // Das verhindert, dass der Pawn überhaupt versucht, den Bill zu reservieren.
    [HarmonyPatch(typeof(Bill), "PawnAllowedToStartAnew")]
    public static class Patch_Bill_PawnAllowed
    {
        [HarmonyPostfix]
        public static void Postfix(Bill __instance, Pawn p, ref bool __result)
        {
            // Wenn Vanilla schon "Nein" sagt, ist es eh egal
            if (!__result) return;

            var extension = __instance.recipe.GetModExtension<ST_RecipeExtension>();
            if (extension == null) return;

            Trait currentTrait = p.story?.traits?.GetTrait(extension.requiredTrait);

            // Fall 1: Trait fehlt komplett
            if (currentTrait == null)
            {
                if (extension.mustHaveTrait) 
                {
                    __result = false;
                    // Optional: Grund für FloatMenu (Rechtsklick) anzeigen
                    JobFailReason.Is("ST_Reason_MissingTrait".Translate(extension.requiredTrait.LabelCap));
                }
                return;
            }

            // Fall 2: Trait da, aber falscher Degree
            if (extension.requiredDegree != -999)
            {
                if (currentTrait.Degree != extension.requiredDegree)
                {
                    __result = false;
                    JobFailReason.Is("ST_Reason_WrongRank".Translate());
                }
            }
        }
    }
}