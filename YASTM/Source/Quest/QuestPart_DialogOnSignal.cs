using Verse;
using RimWorld;

namespace YASTM.Source.Quest
{

    public class QuestPart_DialogOnSignal : QuestPart
    {
        public string inSignal;      
        public string titleKey;      
        public string textKey;       
        public string optionALabel;   
        public string optionASignal;  
        public string optionBLabel;  
        public string optionBSignal; 

        public override void Notify_QuestSignalReceived(Signal signal)
        {

            if (signal.tag != inSignal && signal.tag != $"Quest{quest.id}.{inSignal}") return;

            var title = titleKey.NullOrEmpty() ? "Starfleet" : titleKey.Translate().ToString();
            var text  = textKey.NullOrEmpty()  ? ""          : textKey.Translate().ToString();

            var node = new DiaNode(text);
            node.options.Add(MakeOption(optionALabel, optionASignal));
            node.options.Add(MakeOption(optionBLabel, optionBSignal));

            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(node, null, true, true, title));
        }

        private DiaOption MakeOption(string labelKey, string outSigRaw)
        {
            var label = labelKey.NullOrEmpty() ? "OK" : labelKey.Translate().ToString();
            var opt = new DiaOption(label);
            opt.action = () =>
            {
                if (outSigRaw.NullOrEmpty()) return;
                var send = $"Quest{quest.id}.{outSigRaw}";
                Find.SignalManager.SendSignal(new Signal(send));
                Log.Message($"[YASTM][CHOICE] sent '{send}'");
            };
            opt.resolveTree = true;
            return opt;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, nameof(inSignal));
            Scribe_Values.Look(ref titleKey, nameof(titleKey));
            Scribe_Values.Look(ref textKey, nameof(textKey));
            Scribe_Values.Look(ref optionALabel, nameof(optionALabel));
            Scribe_Values.Look(ref optionASignal, nameof(optionASignal));
            Scribe_Values.Look(ref optionBLabel, nameof(optionBLabel));
            Scribe_Values.Look(ref optionBSignal, nameof(optionBSignal));
        }
    }
}

