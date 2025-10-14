using RimWorld;
using Verse;

namespace StarTrekFactions.QuestParts
{
    public class QuestPart_RunIncidentOnSignal : QuestPartActivable
    {
        public string inSignalRaw;        // z.B. "STQ.Obelisks.DataTransmitted"
        public IncidentDef incidentDef;   // z.B. IncidentDefOf.ManhunterPack
        public float pointsFactor = 1f;
        public int targetMapId = -1;      // optional: feste Map

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (incidentDef == null || inSignalRaw.NullOrEmpty()) return;

            // akzeptiere sowohl raw als auch quest-gescopedes Signal
            string scoped = $"Quest{quest.id}.{inSignalRaw}";
            if (signal.tag != inSignalRaw && signal.tag != scoped) return;

            // Map finden: SUBJECT.Map > targetMapId > erste Spieler-Home-Map
            Map map = null;
            if (signal.args.TryGetArg("SUBJECT", out Thing subj) && subj?.Map != null)
                map = subj.Map;
            if (map == null && targetMapId >= 0)
                map = Find.Maps.Find(m => m.uniqueID == targetMapId);
            if (map == null)
                map = Find.Maps.Find(m => m.IsPlayerHome);
            if (map == null) return;

            var parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
            if (pointsFactor > 0f) parms.points *= pointsFactor;
            parms.forced = true;

            bool ok = incidentDef.Worker.TryExecute(parms);
            Log.Message($"[YASTM][INCIDENT] '{incidentDef.defName}' on map='{map}' fired={ok} via signal='{signal.tag}' (quest {quest.id}).");
        }
    }
}
