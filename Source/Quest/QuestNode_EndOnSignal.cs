// Source/Quest/QuestNode_EndOnSignal.cs
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_EndOnSignal : QuestNode
    {
        public string inSignal;
        // Allowed: "Success", "Fail", "Unknown" (auch "Undefined"/"Neutral" werden als Unknown geparst)
        public string outcome = "Success";

        protected override void RunInt()
        {
            var p = new QuestPart_EndOnSignal
            {
                inSignalRaw    = inSignal,
                inSignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(inSignal),
                outcome        = outcome
            };
            QuestGen.quest.AddPart(p);
        }

        protected override bool TestRunInt(Slate slate) => !inSignal.NullOrEmpty();
    }

    public class QuestPart_EndOnSignal : QuestPart
    {
        public string inSignalRaw, inSignalScoped;
        public string outcome = "Success";

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == inSignalRaw || signal.tag == inSignalScoped)
            {
                quest.End(ParseOutcome(outcome));
            }
        }

        private static QuestEndOutcome ParseOutcome(string s)
        {
            if (s == null) return QuestEndOutcome.Success;
            switch (s.Trim().ToLowerInvariant())
            {
                case "fail":
                case "failed":
                    return QuestEndOutcome.Fail;
                case "unknown":
                case "undefined":
                case "neutral":
                    return QuestEndOutcome.Unknown;
                default:
                    return QuestEndOutcome.Success;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Values.Look(ref outcome, "outcome", "Success");
        }
    }
}
