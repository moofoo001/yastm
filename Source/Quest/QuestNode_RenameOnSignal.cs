// File: Source/Quest/QuestNode_RenameOnSignal.cs
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    // Node: registriert ein QuestPart, das bei inSignal Titel/Beschreibung setzt.
    public class QuestNode_RenameOnSignal : QuestNode
    {
        public string inSignal;
        public string titleKey;        // lokalisierte Keys erlaubt
        public string descriptionKey;

        protected override void RunInt()
        {
            var q = QuestGen.quest;
            var part = new QuestPart_RenameOnSignal
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal),
                title = titleKey.NullOrEmpty() ? null : titleKey.Translate(),
                description = descriptionKey.NullOrEmpty() ? null : descriptionKey.Translate()
            };
            q.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate) => true;
    }

    // Part: Setzt bei Signal den Questnamen/-beschreibung.
    public class QuestPart_RenameOnSignal : QuestPart
    {
        public string inSignal;
        public string title;
        public string description;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == inSignal)
            {
                if (!title.NullOrEmpty()) quest.name = title;
                if (!description.NullOrEmpty()) quest.description = description;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref description, "description");
        }
    }
}
