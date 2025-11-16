using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    public class MapComponent_CommsProgress : MapComponent
    {
        // Cooldowns we use from gizmos
        public int NextAidAllowedTick = -1;
        public int NextContactAllowedTick = -1;

        // (optional book-keeping; kept for save-compat even if not strictly needed)
        private HashSet<int> starfleetQuestIds = new HashSet<int>();

        public MapComponent_CommsProgress(Map map) : base(map) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref NextAidAllowedTick, "NextAidAllowedTick", -1);
            Scribe_Values.Look(ref NextContactAllowedTick, "NextContactAllowedTick", -1);
            Scribe_Collections.Look(ref starfleetQuestIds, "starfleetQuestIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && starfleetQuestIds == null)
                starfleetQuestIds = new HashSet<int>();
        }

        public void RegisterStarfleetQuest(Quest q)
        {
            if (q != null) starfleetQuestIds.Add(q.id);
        }

        /// <summary>Counts ALL ongoing quests regardless of source.</summary>
        public static int CountActiveQuestsAllSources()
        {
            return Find.QuestManager.QuestsListForReading.Count(q => q.State == QuestState.Ongoing);
        }
    }
}
