using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace YASTM.Source.Quest
{
    public class QuestNode_RenameOnSignal : QuestNode
    {
        public string inSignal;    
        public string inSignalRaw; 
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
}