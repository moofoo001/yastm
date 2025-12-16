using System.Linq;
using RimWorld;
using Verse;

namespace StarTrekFactions.Thoughts
{
    /// <summary>
    /// Raktajino mood: everyone gets a small buff,
    /// Starfleet gets a big one, Klingons a moderate one.
    /// </summary>
    public class Thought_Memory_Raktajino : Thought_Memory
    {
        private const string StarfleetMemeDefName = "ST_Meme_Starfleet";
        private const string KlingonMemeDefName = "ST_Meme_KlingonHonor";

        public override float MoodOffset()
        {
            // Base mood from XML stage
            float baseOffset = this.def.stages[CurStageIndex].baseMoodEffect;
            Pawn pawn = this.pawn;

            if (pawn?.Ideo == null || pawn.Ideo.memes == null)
            {
                return baseOffset;
            }

            var memes = pawn.Ideo.memes;

            // Starfleet: raktajino is basically cultural fuel
            if (memes.Any(m => m.defName == StarfleetMemeDefName))
            {
                return baseOffset + 3f; // total +5
            }

            // Klingon honor meme: they appreciate their own coffee blend
            if (memes.Any(m => m.defName == KlingonMemeDefName))
            {
                return baseOffset + 1f; // total +3
            }

            // Others just get the base boost
            return baseOffset;
        }
    }

    /// <summary>
    /// Luxury replicated meal: good for everyone,
    /// extra satisfying for Ferengi commerce ideologies.
    /// </summary>
    public class Thought_Memory_ReplicatedLuxuryMeal : Thought_Memory
    {
        private const string FerengiMemeDefName = "ST_Meme_FerengiCommerce";

        public override float MoodOffset()
        {
            float baseOffset = this.def.stages[CurStageIndex].baseMoodEffect;
            Pawn pawn = this.pawn;

            if (pawn?.Ideo == null || pawn.Ideo.memes == null)
            {
                return baseOffset;
            }

            var memes = pawn.Ideo.memes;

            // Ferengi: profit and luxury are a sign of favor
            if (memes.Any(m => m.defName == FerengiMemeDefName))
            {
                return baseOffset + 3f; // e.g. base 5 -> total +8
            }

            return baseOffset;
        }
    }
}
