using Verse;
using RimWorld;

namespace StarTrekFactions.QuestParts
{
    // Opens a two-option dialog when a (raw or scoped) signal fires,
    // and emits the chosen out-signal (quest-scoped).
    public class QuestPart_DialogOnSignal : QuestPart
    {
        public string inSignal;       // e.g. "STQ.Obelisks.II.Cleared"
        public string titleKey;       // optional dialog title (Keyed)
        public string textKey;        // body text (Keyed)
        public string optionALabel;   // Keyed label
        public string optionASignal;  // out signal (raw, will be quest-scoped)
        public string optionBLabel;   // Keyed label
        public string optionBSignal;  // out signal (raw, will be quest-scoped)

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            // accept raw + quest-scoped
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
