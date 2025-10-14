// Source/Quest/QuestPart_RenameOnSignal.cs
using Verse;
using RimWorld;

namespace StarTrekFactions.QuestParts
{
    // Renames the current quest when a signal fires (raw + scoped tags accepted).
    public class QuestPart_RenameOnSignal : QuestPart
    {
        public string inSignal;          // e.g. "STQ.Obelisks.II.Cleared"
        public string nameKey;           // e.g. "STQ.Obelisks.PhaseIII.Title" (Keyed)
        public string descriptionKey;    // optional: Keyed description

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignal && signal.tag != $"Quest{quest.id}.{inSignal}")
                return;

            // Convert TaggedString -> string to avoid CS0172
            string newName = quest.name;
            if (!nameKey.NullOrEmpty())
                newName = nameKey.Translate().ToString().CapitalizeFirst(); // or: nameKey.TranslateSimple().CapitalizeFirst()

            quest.name = newName;

            if (!descriptionKey.NullOrEmpty())
                quest.description = descriptionKey.Translate().ToString();   // or: descriptionKey.TranslateSimple()

            Log.Message($"[YASTM][RENAME] quest {quest.id} → '{quest.name}' (sig='{signal.tag}')");
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, nameof(inSignal));
            Scribe_Values.Look(ref nameKey, nameof(nameKey));
            Scribe_Values.Look(ref descriptionKey, nameof(descriptionKey));
        }
    }
}
