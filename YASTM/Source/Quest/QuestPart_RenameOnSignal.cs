using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace YASTM.Source.Quest
{
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
            base.ExposeData();
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref description, "description");
        }
    }
}