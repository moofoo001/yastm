using System.Linq;
using RimWorld;
using Verse;
using YASTM.Relics;   // for GameComponent_KahlessRelic

namespace YASTM.Thoughts
{
    /// <summary>
    /// Mood buff for Klingon-aligned pawns if the colony owns the Sword of Kahless.
    /// </summary>
    public class ThoughtWorker_SwordOfKahless : ThoughtWorker
    {
        private const string KlingonMemeDefName = "ST_Meme_KlingonHonor";

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p == null || !p.RaceProps.Humanlike)
                return ThoughtState.Inactive;

            if (p.Faction != Faction.OfPlayer)
                return ThoughtState.Inactive;

            if (!ModsConfig.IdeologyActive)
                return ThoughtState.Inactive;

            var ideo = p.Ideo;
            if (ideo == null || ideo.memes == null)
                return ThoughtState.Inactive;

            // Only Klingon honor ideology gets the buff
            bool hasKlingonMeme = ideo.memes.Any(m => m.defName == KlingonMemeDefName);
            if (!hasKlingonMeme)
                return ThoughtState.Inactive;

            var comp = GameComponent_KahlessRelic.Instance;
            if (comp == null || !comp.swordOfKahlessOwned)
                return ThoughtState.Inactive;

            // All conditions met → buff active
            return ThoughtState.ActiveDefault;
        }
    }
}
