// Source/Quest/QuestPart_RenameOnSignal.cs
using Verse;
using RimWorld;

namespace StarTrekFactions.QuestParts
{

    public class QuestPart_RenameOnSignal : QuestPart
    {
        public string inSignal;          
        public string nameKey;          
        public string descriptionKey;  

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignal && signal.tag != $"Quest{quest.id}.{inSignal}")
                return;


            string newName = quest.name;
            if (!nameKey.NullOrEmpty())
                newName = nameKey.Translate().ToString().CapitalizeFirst(); 

            quest.name = newName;

            if (!descriptionKey.NullOrEmpty())
                quest.description = descriptionKey.Translate().ToString();  

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

