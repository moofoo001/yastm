using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace YASTM.Source.Quest
{
    public class QuestNode_RunIncidentNow : QuestNode
    {
        public string inSignal;      
        public string inSignalRaw;   

        public IncidentDef incident;  
        public IncidentDef incidentDef;
        public float pointsFactor = 1f;

        public string outSignal;

        public string forcedFactionDef;            
        public PawnsArrivalModeDef arrivalMode;     
        public RaidStrategyDef raidStrategy;      

        protected override void RunInt()
        {
            string raw = inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw;

            var part = new QuestPart_RunIncidentOnSignal
            {
                inSignalRaw     = raw,
                inSignalScoped  = QuestGenUtility.HardcodedSignalWithQuestID(raw),
                outSignalRaw    = outSignal,
                outSignalScoped = outSignal.NullOrEmpty() ? null : QuestGenUtility.HardcodedSignalWithQuestID(outSignal),

                incident     = incidentDef ?? incident,
                pointsFactor = pointsFactor,

                forcedFactionDef = forcedFactionDef,
                arrivalMode      = arrivalMode,
                raidStrategy     = raidStrategy
            };

            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate)
            => !(inSignal.NullOrEmpty() && inSignalRaw.NullOrEmpty())
               && (incidentDef != null || incident != null);
    }
}