using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using YASTM; 

namespace YASTM
{
    // 1. DAS IST NEU: Die Definition für unser XML-Preisschild
    public class ReplicatorCostExtension : DefModExtension
    {
        public float cost = 5.0f; // Standardwert, falls im XML nichts steht
    }

    // Die Helper-Klasse bleibt gleich
    public static class TankFinder
    {
        public static CompMatterTank FindNearestValidTank(Thing replicator, float amountRequired)
        {
            if (replicator?.Map?.listerBuildings == null) return null;
            var converters = replicator.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("ST_MatterConverter"));
            Thing bestTank = null;
            float bestDist = 999f;

            foreach (var building in converters)
            {
                float dist = building.Position.DistanceTo(replicator.Position);
                if (dist <= 15f)
                {
                    var tankComp = building.TryGetComp<CompMatterTank>();
                    if (tankComp != null && tankComp.storedMatter >= amountRequired && dist < bestDist)
                    {
                        bestDist = dist;
                        bestTank = building;
                    }
                }
            }
            return bestTank?.TryGetComp<CompMatterTank>();
        }
    }
}

namespace YASTM.Source.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class Patch_Check
    {
        static Patch_Check()
        {
            Log.Message("[YASTM] >>> XML PRICING SYSTEM ONLINE <<<");
        }
    }

    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class Patch_ReplicatorCost
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, object[] __args)
        {
            List<Thing> inputs = __result != null ? __result.ToList() : new List<Thing>();
            
            RecipeDef recipe = (RecipeDef)__args[0];
            IBillGiver billGiver = (__args.Length > 4) ? (__args[4] as IBillGiver) : null;

            if (billGiver != null && billGiver is Thing t && t.def.defName == "ST_Replicator")
            {
                // === PREIS FINDUNG (DAS IST NEU) ===
                float cost = 5.0f; // Der Fallback-Preis, wenn im XML nichts steht

                // Wir fragen das Rezept: "Hast du eine ReplicatorCostExtension?"
                var extension = recipe.GetModExtension<ReplicatorCostExtension>();
                
                if (extension != null)
                {
                    cost = extension.cost;
                    // Debug (optional):
                    Log.Message($"[YASTM] Recipe {recipe.defName} has custom cost: {cost}");
                }
                // ===================================

                var tank = TankFinder.FindNearestValidTank(t, cost);
                
                if (tank != null)
                {
                    if (tank.TryConsume(cost))
                    {
                        MoteMaker.ThrowText(t.DrawPos, t.Map, $"-{cost} Matter", UnityEngine.Color.red);
                    }
                    else
                    {
                         // Sollte durch Finder verhindert werden, aber sicher ist sicher
                        Log.Error($"[YASTM] Tank found but insufficient matter ({tank.storedMatter} < {cost})");
                    }
                }
                else
                {
                    MoteMaker.ThrowText(t.DrawPos, t.Map, "NO MATTER SOURCE!", UnityEngine.Color.red);
                }
            }

            foreach (var p in inputs) yield return p;
        }
    }
}