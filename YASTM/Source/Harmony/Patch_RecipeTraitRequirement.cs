using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace YASTM
{
    // Wir patchen die Prüfung, ob ein Pawn einen Bill (Rezept) starten darf
    [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing")]
    public static class Patch_RecipeTraitRequirement
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Thing thing, bool forced, ref Job __result)
        {
            // Wenn das Ding kein Gebäude mit Bills ist, ignorieren wir es
            if (thing is not IBillGiver billGiver) return true;

            // Wir schauen uns alle Bills an
            foreach (Bill bill in billGiver.BillStack)
            {
                // Hat das Rezept unsere Extension?
                var extension = bill.recipe.GetModExtension<ST_RecipeExtension>();
                
                if (extension != null && extension.mustHaveTrait)
                {
                    // Prüfung: Hat der Pawn den Trait?
                    Trait currentTrait = pawn.story?.traits?.GetTrait(extension.requiredTrait);

                    if (currentTrait == null)
                    {

                        continue; 
                    }

                    // Prüfung: Stimmt der Degree?
                    if (extension.requiredDegree != -999)
                    {
                        if (currentTrait.Degree != extension.requiredDegree)
                        {
                            // Pawn hat falschen Rang (z.B. schon Ace, oder noch kein Cadet)
                            continue; // Dieser Bill ist für diesen Pawn nicht gültig
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