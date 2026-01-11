using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System;

namespace YASTM.Source.HarmonyPatches
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class Patch_MatterConverter
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, object[] __args)
        {
            List<Thing> inputs = __result != null ? __result.ToList() : new List<Thing>();
            
            IBillGiver billGiver = null;
            if (__args != null)
            {
                billGiver = __args.FirstOrDefault(x => x is IBillGiver) as IBillGiver;
            }

            // check if the billGiver is a Matter Converter
            if (billGiver is Thing thing && thing.def.defName == "ST_MatterConverter")
            {
                var tank = thing.TryGetComp<CompRefuelable>();
                
                if (tank != null)
                {
                    // Fill the tank with the produced matter
                    foreach (var p in inputs)
                    {
                        // Refuel the tank with the produced item
                        tank.Refuel(p.stackCount);
                    }


                    // Remove the produced items from output
                    yield break; 
                }
            }

            foreach (var item in inputs) yield return item;
        }
    }
}