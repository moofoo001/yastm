using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{

    public class QuestNode_ChoiceDialogOnSignalV2 : QuestNode
    {
        public string inSignal;

        public string titleKey;
        public string textKey;

        public string optionALabel;
        public string optionASignal;

        public string optionBLabel;
        public string optionBSignal;


        public string choiceMadeSignal = "STQ.Obelisks.Path.ChoiceMade";

        protected override void RunInt()
        {
            var p = new QuestPart_ChoiceDialogOnSignal
            {
                inSignalRaw    = inSignal,
                inSignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(inSignal),

                titleKey = titleKey,
                textKey  = textKey,

                optionALabelKey     = optionALabel,
                optionASignalRaw    = optionASignal,
                optionASignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(optionASignal),

                optionBLabelKey     = optionBLabel,
                optionBSignalRaw    = optionBSignal,
                optionBSignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(optionBSignal),

                choiceMadeSignalRaw    = choiceMadeSignal,
                choiceMadeSignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(choiceMadeSignal)
            };

            QuestGen.quest.AddPart(p);
        }

        protected override bool TestRunInt(Slate slate)
            => !inSignal.NullOrEmpty() && !optionASignal.NullOrEmpty() && !optionBSignal.NullOrEmpty();
    }
}

