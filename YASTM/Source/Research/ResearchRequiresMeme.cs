using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM.Research
{
    /// <summary>
    /// Mod extension to gate research by required memes (Ideology).
    /// If the player's primary ideo does not have at least one of these memes,
    /// the research project cannot be started.
    /// </summary>
    public class ResearchRequiresMeme : DefModExtension
    {
        /// <summary>
        /// At least one of these memes must be present in the player's primary ideo.
        /// </summary>
        public List<MemeDef> requiredMemes;
    }
}
