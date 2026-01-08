using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    // EVENT 1: Kills (Combat Points & Honor)
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_Career_Kill
    {
        public static void Postfix(DamageInfo? dinfo, Pawn __instance)
        {
            if (dinfo.HasValue && dinfo.Value.Instigator is Pawn killer)
            {
                if (__instance.DevelopmentalStage != DevelopmentalStage.Adult) return;
                
                // Ignoriere eigene Fraktion (Friendly Fire gibt keine Punkte)
                if (killer.Faction == __instance.Faction) return; 

                var comp = killer.TryGetComp<CompCareer>();
                if (comp != null)
                {
                    // Standard Kill Punkt
                    comp.AddCareerPoint("CombatKill", 1);

                    // Spezial: Klingonen Ehren-Kill (Keine Kleintiere, keine wehrlosen)
                    if (!__instance.Downed && __instance.BodySize >= 0.8f) 
                    {
                        comp.AddCareerPoint("HonorKill", 1);
                    }
                }
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
                        volume += t.GetPriceFor(t.ActionToDo) * Mathf.Abs(t.CountToTransfer);
                    }
                }

                if (volume > 0)
                {
                    var comp = negotiator.TryGetComp<CompCareer>();
                    if (comp != null)
                    {
                        // 1 Punkt pro Deal
                        comp.AddCareerPoint("SuccessfulTrade", 1);
                        
                        // 1 Punkt pro 500 Silber Wert (für Ferengi Huckster Aufstieg)
                        int profitPoints = Mathf.FloorToInt(volume / 500f);
                        if (profitPoints > 0)
                        {
                            comp.AddCareerPoint("ProfitVolume", profitPoints);
                        }
                    }
                }
            }
        }
    }
    
    // EVENT 3: Skill Level Up (Trigger Check)
    [HarmonyPatch(typeof(SkillRecord), "Learn")]
    public static class Patch_Career_SkillLearn
    {
        public static void Postfix(SkillRecord __instance, float xp, bool direct)
        {
            if (xp > 0 && __instance.Pawn != null)
            {
                // Wir checken nur selten, um Performance zu sparen (z.B. nur bei Level Up wäre besser, aber Hook ist schwerer)
                // Hier einfach: Alle 1000 Learn-Calls mal checken oder comp.TryPromote() ist eh billig.
                if (Find.TickManager.TicksGame % 600 == 0) // Alle 10 Sekunden
                {
                    __instance.Pawn.TryGetComp<CompCareer>()?.TryPromote();
                }
            }
        }
    }
}