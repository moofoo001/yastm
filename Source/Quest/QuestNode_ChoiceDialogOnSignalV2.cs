// File: Source/Quest/QuestNode_ChoiceDialogOnSignalV2.cs
using RimWorld;
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

        protected override void RunInt()
        {
            var part = new QuestPart_DialogOnSignalTwoOptions
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal),
                title = titleKey.Translate(),
                text = textKey.Translate(),
                optionALabel = optionALabel.Translate(),
                optionASignal = optionASignal,
                optionBLabel = optionBLabel.Translate(),
                optionBSignal = optionBSignal
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate) =>
            !optionASignal.NullOrEmpty() && !optionBSignal.NullOrEmpty();
    }

    public class QuestPart_DialogOnSignalTwoOptions : QuestPart
    {
        public string inSignal;
        public string title;
        public string text;
        public string optionALabel;
        public string optionASignal;
        public string optionBLabel;
        public string optionBSignal;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignal) return;

            var root = new DiaNode(text ?? "");
            var optA = new DiaOption(optionALabel.NullOrEmpty() ? "Option A" : optionALabel)
            {
                action = () => { if (!optionASignal.NullOrEmpty()) Find.SignalManager.SendSignal(new Signal(optionASignal)); },
                resolveTree = true
            };
            var optB = new DiaOption(optionBLabel.NullOrEmpty() ? "Option B" : optionBLabel)
            {
                action = () => { if (!optionBSignal.NullOrEmpty()) Find.SignalManager.SendSignal(new Signal(optionBSignal)); },
                resolveTree = true
            };
            root.options.Add(optA);
            root.options.Add(optB);

            Find.WindowStack.Add(new Dialog_NodeTree(root, true, true, title ?? ""));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref text, "text");
            Scribe_Values.Look(ref optionALabel, "optionALabel");
            Scribe_Values.Look(ref optionASignal, "optionASignal");
            Scribe_Values.Look(ref optionBLabel, "optionBLabel");
            Scribe_Values.Look(ref optionBSignal, "optionBSignal");
        }
    }
}
