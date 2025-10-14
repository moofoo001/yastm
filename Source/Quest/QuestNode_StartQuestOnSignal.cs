using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_StartQuestOnSignal : QuestNode
    {
        public string inSignal;
        public string inSignalRaw;
        public QuestScriptDef questDef;

        protected override void RunInt()
        {
            string raw = inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw;
            var part = new QuestPart_StartQuestOnSignal
            {
                inSignalRaw    = raw,
                inSignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(raw),
                questDef       = questDef
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate) => questDef != null && !(inSignal.NullOrEmpty() && inSignalRaw.NullOrEmpty());
    }

    public class QuestPart_StartQuestOnSignal : QuestPart
    {
        public string inSignalRaw, inSignalScoped;
        public QuestScriptDef questDef;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignalRaw && signal.tag != inSignalScoped) return;
            var slate = new Slate();
            QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Defs.Look(ref questDef, "questDef");
        }
    }
}
