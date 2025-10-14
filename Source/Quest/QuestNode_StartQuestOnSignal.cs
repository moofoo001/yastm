// File: Source/Quest/QuestNode_StartQuestOnSignal.cs
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    // Startet einen anderen QuestScriptDef, sobald ein Signal eintrifft.
    public class QuestNode_StartQuestOnSignal : QuestNode
    {
        public string inSignal;
        public QuestScriptDef questDef;

        protected override void RunInt()
        {
            var part = new QuestPart_StartQuestOnSignal
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal),
                questDef = questDef
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate) => questDef != null;
    }

    public class QuestPart_StartQuestOnSignal : QuestPart
    {
        public string inSignal;
        public QuestScriptDef questDef;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == inSignal && questDef != null)
            {
                var slate = new Slate();
                QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Defs.Look(ref questDef, "questDef");
        }
    }
}
