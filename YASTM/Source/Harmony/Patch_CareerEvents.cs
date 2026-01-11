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
                
                // Ignoriere friendly faction kills
                if (killer.Faction == __instance.Faction) return; 

                var comp = killer.TryGetComp<CompCareer>();
                if (comp != null)
                {
                    // Standard kill points
                    comp.AddCareerPoint("CombatKill", 1);

                    // Honor Kills (only for big pawns) / Klingon Honor Code
                    if (!__instance.Downed && __instance.BodySize >= 0.8f) 
                    {
                        comp.AddCareerPoint("HonorKill", 1);
                    }
                }
            }
        }
    }

    // Trade Event (Trade Points)
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
                        // points per trade
                        comp.AddCareerPoint("SuccessfulTrade", 1);
                        
                        // points per profit volume (1 point per 500 silver traded)
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
    
    // Skill Learn Event (Skill Points)
    [HarmonyPatch(typeof(SkillRecord), "Learn")]
    public static class Patch_Career_SkillLearn
    {
        public static void Postfix(SkillRecord __instance, float xp, bool direct)
        {
            if (xp > 0 && __instance.Pawn != null)
            {
                // only for adult pawns

                if (Find.TickManager.TicksGame % 600 == 0)
                {
                    __instance.Pawn.TryGetComp<CompCareer>()?.TryPromote();
                }
            }
        }
    }
}