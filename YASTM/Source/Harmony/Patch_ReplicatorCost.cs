using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using YASTM.Source.Comps;
using System;

namespace YASTM.Source.HarmonyPatches
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class Patch_ReplicatorCost
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, object[] __args)
        {
            List<Thing> inputs = __result != null ? __result.ToList() : new List<Thing>();
            bool shouldReturnItems = true; // Standard: Alles zurückgeben

            try
            {
                IBillGiver billGiver = __args.FirstOrDefault(x => x is IBillGiver) as IBillGiver;
                RecipeDef recipeDef = __args.FirstOrDefault(x => x is RecipeDef) as RecipeDef;

                if (billGiver is Thing thing && thing.def.defName == "ST_Replicator")
                {
                    float cost = (recipeDef != null && recipeDef.defName.Contains("Meal")) ? 1.0f : 5.0f;
                    bool paid = false;

                    var linkComp = thing.TryGetComp<CompAffectedByFacilities>();
                    if (linkComp != null)
                    {
                        foreach (Thing facility in linkComp.LinkedFacilitiesListForReading)
                        {
                            var tank = facility.TryGetComp<CompMatterTank>();
                            if (tank != null && tank.TryConsume(cost))
                            {
                                paid = true;
                                break;
                            }
                        }
                    }

                    if (!paid)
                    {
                        // Nicht bezahlt? Dann nichts ausgeben!
                        shouldReturnItems = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[YASTM] Replicator Patch Error: {ex.Message}");
            }

            // Ausgabe außerhalb des Try-Blocks
            if (shouldReturnItems)
            {
                foreach (var p in inputs) yield return p;
            }
        }
    }
}