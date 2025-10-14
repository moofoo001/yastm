using Verse;
using RimWorld;

namespace StarTrekFactions.QuestParts
{
    // Shows a two-option choice dialog when 'inSignal' is received.
    // Version: RW 1.4/1.5/1.6 kompatibel, ohne PostAdd/QuestGenUtility.
    public class QuestPart_ChoiceDialogOnSignal : QuestPart
    {
        public string inSignal;            // e.g. STQ.Obelisks.III.Choices.Ready

        // Text keys (Keyed)
        public string titleKey;            // e.g. STQ.Obelisks.PhaseIII.ChoiceTitle
        public string textKey;             // e.g. STQ.Obelisks.PhaseIII.ChoiceText

        // Option A
        public string optionALabel;        // e.g. STQ.Obelisks.Path.Warden.Label
        public string optionASignal;       // e.g. STQ.Obelisks.Path.Warden.Chosen

        // Option B
        public string optionBLabel;        // e.g. STQ.Obelisks.Path.Vault.Label
        public string optionBSignal;       // e.g. STQ.Obelisks.Path.Vault.Chosen

        private bool shown;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (shown) return;
            if (!Matches(signal.tag, inSignal)) return;

            shown = true;

            // Sauber mit TaggedString arbeiten, um CS0172 zu vermeiden
            TaggedString body  = string.IsNullOrEmpty(textKey)
                ? (TaggedString)"Select how to proceed."
                : textKey.Translate();

            TaggedString title = string.IsNullOrEmpty(titleKey)
                ? (TaggedString)"Directive required"
                : titleKey.Translate();

            var root = new DiaNode(body);

            // Labels als string auflösen
            string aLabel = string.IsNullOrEmpty(optionALabel) ? "OK"     : optionALabel.Translate().ToString();
            string bLabel = string.IsNullOrEmpty(optionBLabel) ? "Cancel" : optionBLabel.Translate().ToString();

            var optA = new DiaOption(aLabel) { resolveTree = true };
            optA.action = () => Fire(optionASignal);
            root.options.Add(optA);

            var optB = new DiaOption(bLabel) { resolveTree = true };
            optB.action = () => Fire(optionBSignal);
            root.options.Add(optB);

            // RW-kompatibel (ohne WithTitle). Wenn du den Titel sichtbar willst, pack ihn in den Text-Key.
            Find.WindowStack.Add(new Dialog_NodeTree(root, true));
        }

        private void Fire(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            // raw
            Find.SignalManager.SendSignal(new Signal(tag));
            // quest-scoped
            var scoped = Scoped(tag);
            if (scoped != tag)
                Find.SignalManager.SendSignal(new Signal(scoped));

            if (Prefs.DevMode)
                Log.Message($"[YASTM][CHOICE] Sent '{tag}' & '{scoped}' (quest {quest?.id})");
        }

        private bool Matches(string got, string want)
        {
            if (string.IsNullOrEmpty(got) || string.IsNullOrEmpty(want)) return false;
            if (got == want) return true;
            return got == Scoped(want);
        }

        private string Scoped(string tag)
        {
            if (string.IsNullOrEmpty(tag) || quest == null) return tag;
            if (tag.StartsWith("Quest")) return tag;
            return $"Quest{quest.id}.{tag}";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal,     nameof(inSignal));
            Scribe_Values.Look(ref titleKey,     nameof(titleKey));
            Scribe_Values.Look(ref textKey,      nameof(textKey));
            Scribe_Values.Look(ref optionALabel, nameof(optionALabel));
            Scribe_Values.Look(ref optionASignal,nameof(optionASignal));
            Scribe_Values.Look(ref optionBLabel, nameof(optionBLabel));
            Scribe_Values.Look(ref optionBSignal,nameof(optionBSignal));
            Scribe_Values.Look(ref shown,        nameof(shown));
        }
    }
}
