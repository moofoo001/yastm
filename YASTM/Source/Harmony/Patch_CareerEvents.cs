using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine; // <--- HIER FEHLTE DER VERWEIS FÜR 'Mathf'

namespace YASTM
{
    // EVENT 1: Kills (Combat Points)
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_Career_Kill
    {
        public static void Postfix(DamageInfo? dinfo, Pawn __instance)
        {
            if (dinfo.HasValue && dinfo.Value.Instigator is Pawn killer)
            {
                if (__instance.DevelopmentalStage != DevelopmentalStage.Adult) return;
                if (__instance.RaceProps.Animal && __instance.BodySize < 0.7f) return;
                if (killer.Faction == __instance.Faction) return; 

                var comp = killer.TryGetComp<CompCareer>();
                comp?.AddPoints("Combat", 1f); 
            }
        }
    }

    // EVENT 2: Handel (Trade Points)
    [HarmonyPatch(typeof(TradeDeal), "TryExecute")]
    public static class Patch_Career_Trade
    {
        public static void Postfix(TradeDeal __instance, bool __result)
        {
            if (__result) 
            {
                Pawn negotiator = TradeSession.playerNegotiator;
                if (negotiator == null) return;

                float volume = 0f;
                foreach (Tradeable t in __instance.AllTradeables)
                {
                    if (t.ActionToDo != TradeAction.None)
                    {
                        // Mathf benötigt 'using UnityEngine;'
                        volume += t.GetPriceFor(t.ActionToDo) * Mathf.Abs(t.CountToTransfer);
                    }
                }

                if (volume > 0)
                {
                    var comp = negotiator.TryGetComp<CompCareer>();
                    comp?.AddPoints("Trade", volume / 10f); 
                }
            }
        }
    }
}