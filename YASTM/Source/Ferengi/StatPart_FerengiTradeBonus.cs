using System.Linq;
using RimWorld;
using Verse;

namespace YASTM.Stats
{
    /// <summary>
    /// Adds a trade price improvement bonus for Ferengi traders
    /// once Ferengi commerce research projects are completed.
    /// </summary>
    public class StatPart_FerengiTradeBonus : StatPart
    {
        private const string FerengiMemeDefName = "ST_Meme_FerengiCommerce";

        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
                return;

            if (!IsFerengiTrader(pawn))
                return;

            float bonus = GetCurrentBonus();
            if (bonus <= 0f)
                return;

            val += bonus;
        }

        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null || !IsFerengiTrader(pawn))
                return null;

            float bonus = GetCurrentBonus();
            if (bonus <= 0f)
                return null;

            return "Ferengi commerce protocols: " + bonus.ToStringPercent();
        }

        /// <summary>
        /// Returns the current trade bonus based on completed Ferengi commerce research.
        /// </summary>
        public static float GetCurrentBonus()
        {
            float bonus = 0f;

            ResearchProjectDef commerce =
                DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Ferengi_Commerce");

            ResearchProjectDef advancedCommerce =
                DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Ferengi_AdvancedCommerce");

            // +4% for basic commerce
            if (commerce != null && commerce.IsFinished)
                bonus += 0.04f;

            // +3% extra for advanced commerce (total +7% if both are done)
            if (advancedCommerce != null && advancedCommerce.IsFinished)
                bonus += 0.03f;

            return bonus;
        }

        /// <summary>
        /// Checks if this pawn should be treated as a Ferengi trader:
        /// currently tied to the Ferengi commerce meme.
        /// </summary>
        public static bool IsFerengiTrader(Pawn pawn)
        {
            if (pawn == null)
                return false;

            if (!ModsConfig.IdeologyActive)
                return false;

            Ideo ideo = pawn.Ideo;
            if (ideo == null || ideo.memes == null)
                return false;

            return ideo.memes.Any(m => m.defName == FerengiMemeDefName);
        }
    }
}
