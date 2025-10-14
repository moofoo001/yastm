using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_RenameOnSignal : QuestNode
    {
        public string inSignal;      // XML: üblich
        public string inSignalRaw;   // XML: optional (Alias)
        public string titleKey;
        public string descriptionKey;

        protected override void RunInt()
        {
            string raw = inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw;
            var part = new QuestPart_RenameOnSignal
            {
                inSignalRaw    = raw,
                inSignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(raw),
                title          = titleKey.NullOrEmpty() ? null : titleKey.Translate(),
                description    = descriptionKey.NullOrEmpty() ? null : descriptionKey.Translate()
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate) => !titleKey.NullOrEmpty() || !descriptionKey.NullOrEmpty();
    }

    public class QuestPart_RenameOnSignal : QuestPart
    {
        public string inSignalRaw;
        public string inSignalScoped;
        public string title;
        public string description;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignalRaw && signal.tag != inSignalScoped) return;
            if (!title.NullOrEmpty()) quest.name = title;
            if (!description.NullOrEmpty()) quest.description = description;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref description, "description");
        }
    }
}
