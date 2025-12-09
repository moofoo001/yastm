using HarmonyLib;
using RimWorld;
using Verse;
using YASTM.Stats;

namespace YASTM.Ferengi
{
    /// <summary>
    /// Adds a Ferengi trade bonus line to the price tooltip in the trade window.
    /// </summary>
    [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.GetPriceTooltip))]
    public static class Tradeable_FerengiTooltip_Patch
    {
        public static void Postfix(ref string __result, TradeAction action)
        {
            // We only care about player buying or selling
            if (action != TradeAction.PlayerBuys && action != TradeAction.PlayerSells)
                return;

            // Use the current trade negotiator
            Pawn negotiator = TradeSession.playerNegotiator;
            if (negotiator == null)
                return;

            // Only apply if this pawn counts as a Ferengi trader
            if (!StatPart_FerengiTradeBonus.IsFerengiTrader(negotiator))
                return;

            float bonus = StatPart_FerengiTradeBonus.GetCurrentBonus();
            if (bonus <= 0f)
                return;

            // Append our custom line to the tooltip
            __result += "\n\n" + "Ferengi trade bonus: " + bonus.ToStringPercent();
        }
    }
}
