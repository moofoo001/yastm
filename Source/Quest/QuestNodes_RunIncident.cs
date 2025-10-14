using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_RunIncidentNow : QuestNode
    {
        public string inSignal;
        public string inSignalRaw;
        public IncidentDef incident;
        public IncidentDef incidentDef;
        public float pointsFactor = 1f;
        public string outSignal; // Basisname ohne QuestID

        protected override void RunInt()
        {
            string raw = inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw;
            var part = new QuestPart_RunIncidentOnSignal
            {
                inSignalRaw    = raw,
                inSignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(raw),
                incident       = incidentDef ?? incident,
                outSignalRaw   = outSignal,
                outSignalScoped= outSignal.NullOrEmpty() ? null : QuestGenUtility.HardcodedSignalWithQuestID(outSignal),
                pointsFactor   = pointsFactor
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate)
            => !(inSignal.NullOrEmpty() && inSignalRaw.NullOrEmpty()) && (incidentDef != null || incident != null);
    }

    public class QuestPart_RunIncidentOnSignal : QuestPart
    {
        public string inSignalRaw, inSignalScoped;
        public IncidentDef incident;
        public string outSignalRaw, outSignalScoped;
        public float pointsFactor = 1f;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignalRaw && signal.tag != inSignalScoped) return;
            Map map = Find.AnyPlayerHomeMap ?? Find.Maps.FirstOrDefault();
            if (map == null || incident == null) return;

            var cat = incident.category ?? IncidentCategoryDefOf.ThreatSmall;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(cat, map);
            parms.forced = true;
            if (pointsFactor != 1f && parms.points > 0f) parms.points *= pointsFactor;

            if (incident.Worker.TryExecute(parms))
            {
                if (!outSignalRaw.NullOrEmpty())    Find.SignalManager.SendSignal(new Signal(outSignalRaw));
                if (!outSignalScoped.NullOrEmpty()) Find.SignalManager.SendSignal(new Signal(outSignalScoped));
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Defs.Look(ref incident, "incident");
            Scribe_Values.Look(ref outSignalRaw, "outSignalRaw");
            Scribe_Values.Look(ref outSignalScoped, "outSignalScoped");
            Scribe_Values.Look(ref pointsFactor, "pointsFactor", 1f);
        }
    }
}
