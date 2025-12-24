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

            // Wir prüfen auf den Standard CompRefuelable
            if (billGiver is Thing thing && thing.def.defName == "ST_MatterConverter")
            {
                var tank = thing.TryGetComp<CompRefuelable>();
                
                if (tank != null)
                {
                    // ALLES was hier produziert wird, landet im Tank
                    foreach (var p in inputs)
                    {
                        // RimWorld Standard-Methode zum Auffüllen
                        tank.Refuel(p.stackCount);
                    }

                    // WICHTIG: Wir geben nichts zurück (yield break).
                    // Das Item wird dadurch "gelöscht" und existiert nur noch als Zahl im Tank.
                    yield break; 
                }
            }

            foreach (var item in inputs) yield return item;
        }
    }
}