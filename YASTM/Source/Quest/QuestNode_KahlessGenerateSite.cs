using RimWorld;              // FactionDef
using RimWorld.Planet;       // SitePartDef
using RimWorld.QuestGen;     // QuestNode, Slate
using Verse;

namespace StarTrekFactions.QuestNodes
{
    /// <summary>
    /// Placeholder quest node for future Kahless world-site generation.
    /// Currently it does nothing except log a message so the quest remains stable.
    /// </summary>
    public class QuestNode_KahlessGenerateSite : QuestNode
    {
        // Kept so XML fields don't break when we later implement real logic
        public string storeSiteAs = "kahlessSite";
        public FactionDef enemyFaction;
        public SitePartDef sitePart;
        public FloatRange pointsRange = new FloatRange(300f, 800f);

        protected override void RunInt()
        {
            // For now this node is a no-op to keep the mod stable.
            Log.Message("[YASTM][Kahless] QuestNode_KahlessGenerateSite placeholder executed (no site generated yet).");
        }

        protected override bool TestRunInt(Slate slate)
        {
            // Always allow the quest to generate for now.
            return true;
        }
    }
}
