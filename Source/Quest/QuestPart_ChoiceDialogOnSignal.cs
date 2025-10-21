using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestPart_ChoiceDialogOnSignal : QuestPart
    {
        public string inSignalRaw, inSignalScoped;

        public string titleKey;
        public string textKey;

        public string optionALabelKey;   
        public string optionBLabelKey;

        public string optionASignalRaw;
        public string optionASignalScoped;

        public string optionBSignalRaw;
        public string optionBSignalScoped;

        public string choiceMadeSignalRaw;    
        public string choiceMadeSignalScoped;

        public bool shown;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (shown) return;

            if (signal.tag == inSignalRaw || signal.tag == inSignalScoped)
            {
                shown = true;

                string title = titleKey.NullOrEmpty() ? "Directive required" : titleKey.Translate();
                string body  = textKey.NullOrEmpty()  ? ""                  : textKey.Translate();

                DiaNode root = new DiaNode(body);

                DiaOption a = new DiaOption(optionALabelKey.Translate());
                a.action = () =>
                {
                    if (!choiceMadeSignalRaw.NullOrEmpty())     Find.SignalManager.SendSignal(new Signal(choiceMadeSignalRaw));
                    if (!choiceMadeSignalScoped.NullOrEmpty())  Find.SignalManager.SendSignal(new Signal(choiceMadeSignalScoped));

                    if (!optionASignalRaw.NullOrEmpty())        Find.SignalManager.SendSignal(new Signal(optionASignalRaw));
                    if (!optionASignalScoped.NullOrEmpty())     Find.SignalManager.SendSignal(new Signal(optionASignalScoped));
                };
                a.resolveTree = true;
                root.options.Add(a);

                DiaOption b = new DiaOption(optionBLabelKey.Translate());
                b.action = () =>
                {
                    if (!choiceMadeSignalRaw.NullOrEmpty())     Find.SignalManager.SendSignal(new Signal(choiceMadeSignalRaw));
                    if (!choiceMadeSignalScoped.NullOrEmpty())  Find.SignalManager.SendSignal(new Signal(choiceMadeSignalScoped));

                    if (!optionBSignalRaw.NullOrEmpty())        Find.SignalManager.SendSignal(new Signal(optionBSignalRaw));
                    if (!optionBSignalScoped.NullOrEmpty())     Find.SignalManager.SendSignal(new Signal(optionBSignalScoped));
                };
                b.resolveTree = true;
                root.options.Add(b);


                root.options.Add(DiaOption.DefaultOK);

                Find.WindowStack.Add(new Dialog_NodeTree(root, true, true, title));
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Values.Look(ref titleKey, "titleKey");
            Scribe_Values.Look(ref textKey, "textKey");
            Scribe_Values.Look(ref optionALabelKey, "optionALabelKey");
            Scribe_Values.Look(ref optionBLabelKey, "optionBLabelKey");
            Scribe_Values.Look(ref optionASignalRaw, "optionASignalRaw");
            Scribe_Values.Look(ref optionASignalScoped, "optionASignalScoped");
            Scribe_Values.Look(ref optionBSignalRaw, "optionBSignalRaw");
            Scribe_Values.Look(ref optionBSignalScoped, "optionBSignalScoped");
            Scribe_Values.Look(ref choiceMadeSignalRaw, "choiceMadeSignalRaw");
            Scribe_Values.Look(ref choiceMadeSignalScoped, "choiceMadeSignalScoped");
            Scribe_Values.Look(ref shown, "shown", false);
        }
    }
}
