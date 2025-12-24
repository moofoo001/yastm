using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using YASTM; 
using System;

namespace YASTM.Source.HarmonyPatches
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class Patch_ReplicatorCost
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, object[] __args)
        {
            List<Thing> inputs = __result != null ? __result.ToList() : new List<Thing>();
            bool shouldReturnItems = true; 

            // Prüfen, ob es der Replikator ist
            IBillGiver billGiver = __args.FirstOrDefault(x => x is IBillGiver) as IBillGiver;
            
            if (billGiver is Thing thing && thing.def.defName == "ST_Replicator")
            {
                Log.Message("[YASTM DEBUG] Replicator finished item. Trying to pay...");

                RecipeDef recipeDef = __args.FirstOrDefault(x => x is RecipeDef) as RecipeDef;
                float cost = (recipeDef != null && recipeDef.defName.Contains("Meal")) ? 1.0f : 5.0f;
                
                bool paid = false;

                // Verbindung suchen
                var linkComp = thing.TryGetComp<CompAffectedByFacilities>();
                if (linkComp == null)
                {
                    Log.Error("[YASTM ERROR] Replicator has no CompAffectedByFacilities! Check XML!");
                }
                else
                {
                    Log.Message($"[YASTM DEBUG] Connected Facilities: {linkComp.LinkedFacilitiesListForReading.Count}");
                    
                    foreach (Thing facility in linkComp.LinkedFacilitiesListForReading)
                    {
                        var tank = facility.TryGetComp<CompMatterTank>();
                        if (tank != null)
                        {
                            Log.Message($"[YASTM DEBUG] Tank found. Content: {tank.storedMatter}/{tank.MaxCapacity}. Need: {cost}");
                            
                            if (tank.TryConsume(cost))
                            {
                                Log.Message("[YASTM DEBUG] Payment successful!");
                                paid = true;
                                break;
                            }
                            else
                            {
                                Log.Warning("[YASTM DEBUG] Not enough matter in this tank.");
                            }
                        }
                    }
                }

                if (!paid)
                {
                    Log.Error("[YASTM ERROR] Payment FAILED. No connected tank had enough matter (or connection missing).");
                    // Wenn Sie wollen, dass die Items NICHT spawnen, wenn nicht bezahlt wurde:
                    // shouldReturnItems = false; 
                }
            }

            if (shouldReturnItems)
            {
                foreach (var p in inputs) yield return p;
            }
        }
    }
}