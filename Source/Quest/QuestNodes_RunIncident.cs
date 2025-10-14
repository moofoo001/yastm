// File: Source/Quest/QuestNodes_RunIncident.cs
using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    // XML/Node-Felder (kompatibel zu deinen bisherigen Namen):
    // - inSignal ODER inSignalRaw
    // - incident ODER incidentDef
    // - pointsFactor (optional, default 1.0)
    // - outSignal (optional)
    public class QuestNode_RunIncidentNow : QuestNode
    {
        public string inSignal;          // Alias 1
        public string inSignalRaw;       // Alias 2 (deine Variante)

        public IncidentDef incident;     // Alias 1
        public IncidentDef incidentDef;  // Alias 2 (deine Variante)

        public float pointsFactor = 1f;  // optionaler Multiplikator für Incident-Punkte
        public string outSignal;         // optionales Follow-up-Signal

        protected override void RunInt()
        {
            var part = new QuestPart_RunIncidentOnSignal
            {
                inSignal    = (inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw),     // bereits mit QuestID gehärtet
                incident    = incidentDef ?? incident,
                outSignal   = outSignal,
                pointsFactor = pointsFactor
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate)
        {
            bool hasSignal = !(inSignalRaw.NullOrEmpty() && inSignal.NullOrEmpty());
            bool hasIncident = (incidentDef != null || incident != null);
            return hasSignal && hasIncident;
        }
    }

    public class QuestPart_RunIncidentOnSignal : QuestPart
    {
        public string inSignal;          // intern: bereits mit QuestID gehärtet
        public IncidentDef incident;     // was ausgeführt wird
        public string outSignal;         // optional: wird nach erfolgreicher Ausführung gesendet
        public float pointsFactor = 1f;  // skaliert Storyteller-Parms.points

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignal || incident == null) return;

            Map map = Find.AnyPlayerHomeMap ?? Find.Maps.FirstOrDefault();
            if (map == null) return;

            var category = incident.category ?? IncidentCategoryDefOf.ThreatSmall;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(category, map);
            parms.forced = true;

            if (pointsFactor != 1f && parms.points > 0f)
                parms.points *= pointsFactor;

            if (incident.Worker.TryExecute(parms))
            {
                if (!outSignal.NullOrEmpty())
                    Find.SignalManager.SendSignal(new Signal(outSignal));
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Defs.Look(ref incident, "incident");
            Scribe_Values.Look(ref outSignal, "outSignal");
            Scribe_Values.Look(ref pointsFactor, "pointsFactor", 1f);
        }
    }
}
