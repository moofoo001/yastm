using System.Linq;
using RimWorld;
using Verse;
using YASTM.Relics;   // GameComponent_KahlessRelic

namespace YASTM.Stats
{
    /// <summary>
    /// Combat bonus for Klingon-aligned pawns when the colony owns the Sword of Kahless.
    /// Applied to MeleeHitChance and MeleeDamageFactor.
    /// </summary>
    public class StatPart_KahlessBattleFervor : StatPart
    {
        private const string KlingonMemeDefName = "ST_Meme_KlingonHonor";

        // Flat multipliers
        private const float HitChanceFactor = 1.15f;   // +15%
        private const float DamageFactor = 1.20f;      // +20%

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!Applies(req))
                return;

            // parentStat sagt uns, für welches Stat wir gerade laufen
            if (parentStat == StatDefOf.MeleeHitChance)
            {
                val *= HitChanceFactor;
            }
            else if (parentStat == StatDefOf.MeleeDamageFactor)
            {
                val *= DamageFactor;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!Applies(req))
                return null;

            if (parentStat == StatDefOf.MeleeHitChance)
            {
                return "Kahless battle fervor: +" + (int)((HitChanceFactor - 1f) * 100f) + "% melee hit chance";
            }

            if (parentStat == StatDefOf.MeleeDamageFactor)
            {
                return "Kahless battle fervor: +" + (int)((DamageFactor - 1f) * 100f) + "% melee damage";
            }

            return null;
        }

        private bool Applies(StatRequest req)
        {
            if (!req.HasThing)
                return false;

            Pawn pawn = req.Thing as Pawn;
            if (pawn == null || !pawn.RaceProps.Humanlike)
                return false;

            if (pawn.Faction != Faction.OfPlayer)
                return false;

            if (!ModsConfig.IdeologyActive)
                return false;

            // Check Klingon meme
            Ideo ideo = pawn.Ideo;
            if (ideo == null || ideo.memes == null)
                return false;

            bool hasKlingonMeme = ideo.memes.Any(m => m.defName == KlingonMemeDefName);
            if (!hasKlingonMeme)
                return false;

            // Check if the colony owns the Sword of Kahless
            var comp = GameComponent_KahlessRelic.Instance;
            if (comp == null || !comp.swordOfKahlessOwned)
                return false;

            return true;
        }
    }
}
