using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace YASTM.Source.Quest
{
    public class QuestNode_WaitEnemiesDefeated : QuestNode
    {
        public string inSignal;
        public string inSignalRaw;
        public bool onlyManhunters = false;
        public string outSignal;

        protected override void RunInt()
        {
            string raw = inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw;
            var part = new QuestPart_WaitEnemiesDefeated
            {
                inSignalRaw     = raw,
                inSignalScoped  = QuestGenUtility.HardcodedSignalWithQuestID(raw),
                onlyManhunters  = onlyManhunters,
                outSignalRaw    = outSignal,
                outSignalScoped = outSignal.NullOrEmpty() ? null : QuestGenUtility.HardcodedSignalWithQuestID(outSignal)
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate)
            => !(inSignal.NullOrEmpty() && inSignalRaw.NullOrEmpty()) && !outSignal.NullOrEmpty();
    }
}