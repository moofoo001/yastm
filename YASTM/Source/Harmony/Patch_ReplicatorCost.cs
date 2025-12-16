using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq; // Wichtig für ToList()

namespace YASTM.Source.HarmonyPatches
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class Patch_ReplicatorCost
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, IBillGiver billGiver, Pawn worker)
        {
            // 1. Wir speichern das Ergebnis erst einmal zwischen (Quarantäne)
            List<Thing> products = __result.ToList();

            // Sicherheitschecks
            if (billGiver == null) 
            {
                foreach (var p in products) yield return p;
                yield break;
            }

            Thing thingGiver = billGiver as Thing;
            if (thingGiver == null) 
            {
                foreach (var p in products) yield return p;
                yield break;
            }

            // 2. Ist es ein Replikator?
            if (thingGiver.def.defName == "ST_Replicator")
            {
                float matterCost = 1.0f;
                // Einfache Kostenberechnung (kann später erweitert werden)
                if (recipeDef != null && !recipeDef.defName.Contains("Meal") && !recipeDef.label.Contains("meal"))
                {
                    matterCost = 5.0f; 
                }

                bool fuelConsumed = false;

                // 3. Suche nach verbundenen Convertern (Netzwerk-Check)
                CompAffectedByFacilities linkComp = thingGiver.TryGetComp<CompAffectedByFacilities>();
                
                if (linkComp != null)
                {
                    foreach (Thing facility in linkComp.LinkedFacilitiesListForReading)
                    {
                        // Prüfen: Ist es ein Converter MIT Tank?
                        CompRefuelable tank = facility.TryGetComp<CompRefuelable>();
                        
                        // Check: Ist genug drin?
                        if (tank != null && tank.Fuel >= matterCost)
                        {
                            // Energie! Abziehen.
                            tank.ConsumeFuel(matterCost);
                            fuelConsumed = true;
                            break; // Kosten gedeckt, Suche beenden
                        }
                    }
                }

                // 4. Die Entscheidung
                if (fuelConsumed)
                {
                    // Materie war da -> Wir materialisieren die Items
                    foreach (var p in products) yield return p;
                }
                else
                {
                    // KEINE Materie -> Replikation fehlgeschlagen!
                    // Wir geben NICHTS zurück (yield return wird übersprungen). 
                    // Das Item verschwindet im Nirwana.
                    
                    Messages.Message("ST_ReplicatorNoMatter".Translate(), thingGiver, MessageTypeDefOf.RejectInput, false);
                    
                    // Optional: Kleiner Sound für Fehler
                    // SoundDefOf.ClickReject.PlayOneShot(thingGiver);
                }
            }
            else
            {
                // Kein Replikator (normale Werkbank) -> Alles normal durchlassen
                foreach (var p in products) yield return p;
            }
        }
    }
}