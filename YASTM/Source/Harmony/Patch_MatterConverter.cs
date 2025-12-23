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
            // result to list
            List<Thing> inputs = __result != null ? __result.ToList() : new List<Thing>();
            
            // find bill giver
            IBillGiver billGiver = null;
            if (__args != null)
            {
                billGiver = __args.FirstOrDefault(x => x is IBillGiver) as IBillGiver;
            }

            // check if bill giver is matter converter
            if (billGiver is Thing thing && thing.def.defName == "ST_MatterConverter")
            {
                var tank = thing.TryGetComp<CompMatterTank>();
                if (tank != null)
                {

                    
                    foreach (var p in inputs)
                    {
                        tank.AddMatter(p.stackCount);
                        
                        // debug 
                        Log.Message($"[YASTM] Converter converted {p.Label} to {p.stackCount} matter.");
                    }

                    yield break; 
                }
            }

            // normal return
            foreach (var item in inputs) yield return item;
        }
    }
}