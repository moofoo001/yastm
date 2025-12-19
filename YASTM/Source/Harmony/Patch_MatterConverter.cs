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
    public static class Patch_MatterConverter
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, object[] __args)
        {
            // 1. Original-Produkte sichern
            List<Thing> inputs = __result != null ? __result.ToList() : new List<Thing>();
            
            // 2. Liste für das Ergebnis vorbereiten
            List<Thing> finalOutput = new List<Thing>();
            bool processed = false;

            // 3. Sichere Verarbeitung im Try-Catch
            try
            {
                IBillGiver billGiver = __args.FirstOrDefault(x => x is IBillGiver) as IBillGiver;

                if (billGiver is Thing thing && thing.def.defName == "ST_MatterConverter")
                {
                    var tank = thing.TryGetComp<CompMatterTank>();
                    if (tank != null)
                    {
                        bool consumed = false;
                        foreach (var p in inputs)
                        {
                            if (p.def.defName == "ST_ReplicatorFeedstock")
                            {
                                // Ab in den Tank
                                tank.AddMatter(p.stackCount);
                                consumed = true;
                            }
                            else
                            {
                                // Kein Feedstock? Dann normal ausgeben
                                finalOutput.Add(p);
                            }
                        }
                        
                        // Wenn wir was in den Tank getan haben, markieren wir als erfolgreich processed
                        // (finalOutput enthält dann nur den Restmüll)
                        if (consumed) processed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[YASTM] MatterConverter Patch Error: {ex.Message}");
            }

            // 4. Ausgabe (Außerhalb des Try-Catch!)
            
            if (processed)
            {
                // Wenn wir verarbeitet haben, geben wir nur den Rest zurück (oder nichts, wenn alles im Tank ist)
                foreach (var item in finalOutput) yield return item;
            }
            else
            {
                // Wenn nichts passiert ist (oder Fehler), geben wir alles original zurück
                foreach (var item in inputs) yield return item;
            }
        }
    }
}