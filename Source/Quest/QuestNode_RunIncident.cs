using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_RunIncidentNow : QuestNode
    {
        public string inSignal;        // scoped vom Def-Lader
        public string inSignalRaw;     // raw (direkt)

        // beides unterstützen (Kompatibilität):
        public IncidentDef incident;   // alt
        public IncidentDef incidentDef;// neu
        public float pointsFactor = 1f;

        // Basisname; wir erzeugen zusätzlich die gescopte Variante
        public string outSignal;

        // NEU
        public string forcedFactionDef;              // z.B. "Romulan_Star_Empire"
        public PawnsArrivalModeDef arrivalMode;     // z.B. CenterDrop / EdgeDrop / EdgeWalkIn
        public RaidStrategyDef raidStrategy;        // z.B. ImmediateAttack

        protected override void RunInt()
        {
            string raw = inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw;

            var part = new QuestPart_RunIncidentOnSignal
            {
                // Signals
                inSignalRaw     = raw,
                inSignalScoped  = QuestGenUtility.HardcodedSignalWithQuestID(raw),
                outSignalRaw    = outSignal,
                outSignalScoped = outSignal.NullOrEmpty()
                                  ? null
                                  : QuestGenUtility.HardcodedSignalWithQuestID(outSignal),

                // Incident + Tuning
                incident     = incidentDef ?? incident,
                pointsFactor = pointsFactor,

                // NEU
                forcedFactionDef = forcedFactionDef,
                arrivalMode      = arrivalMode,
                raidStrategy     = raidStrategy
            };

            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate)
            => !(inSignal.NullOrEmpty() && inSignalRaw.NullOrEmpty())
               && (incidentDef != null || incident != null);

        // WICHTIG: KEIN ExposeData() hier – Nodes werden nicht gescribed.
    }

    public class QuestPart_RunIncidentOnSignal : QuestPart
    {
        // Signals (raw + scoped)
        public string inSignalRaw, inSignalScoped;
        public string outSignalRaw, outSignalScoped;

        // Incident + Tuning
        public IncidentDef incident;
        public float pointsFactor = 1f;

        // NEU
        public string forcedFactionDef;
        public PawnsArrivalModeDef arrivalMode;
        public RaidStrategyDef raidStrategy;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignalRaw && signal.tag != inSignalScoped) return;
            if (incident == null) return;

            Map map = Find.AnyPlayerHomeMap ?? Find.Maps.FirstOrDefault();
            if (map == null) return;

            var cat   = incident.category ?? IncidentCategoryDefOf.ThreatSmall;
            var parms = StorytellerUtility.DefaultParmsNow(cat, map);
            parms.forced = true;

            if (pointsFactor != 1f && parms.points > 0f)
                parms.points *= pointsFactor;

            // Fraktion erzwingen (Romulaner etc.)
            if (!forcedFactionDef.NullOrEmpty())
            {
                var facDef = DefDatabase<FactionDef>.GetNamedSilentFail(forcedFactionDef);
                if (facDef != null)
                {
                    Faction fac = Find.FactionManager.AllFactions
                        .FirstOrDefault(f => f.def == facDef && !f.IsPlayer);

                    // falls nicht vorhanden – optional erzeugen (1.6: Parms-Objekt)
                    if (fac == null)
                    {
                        var fgParms = new FactionGeneratorParms();
                        fgParms.factionDef = facDef;

                        fac = FactionGenerator.NewGeneratedFaction(fgParms);
                        if (fac != null && !Find.FactionManager.AllFactions.Contains(fac))
                        {
                            Find.FactionManager.Add(fac);
                        }
                    }

                    if (fac != null)
                        parms.faction = fac;

                    // (Hostility NICHT hier erzwingen; stelle das lieber im FactionDef oder Szenario ein)
                }
            }

            // Ankunft/Strategie (optional)
            if (arrivalMode != null) parms.raidArrivalMode = arrivalMode;   // CenterDrop, EdgeDrop, EdgeWalkIn …
            if (raidStrategy != null) parms.raidStrategy   = raidStrategy;  // ImmediateAttack, StageThenAttack …

            if (incident.Worker.TryExecute(parms))
            {
                if (!outSignalRaw.NullOrEmpty())
                    Find.SignalManager.SendSignal(new Signal(outSignalRaw));
                if (!outSignalScoped.NullOrEmpty())
                    Find.SignalManager.SendSignal(new Signal(outSignalScoped));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Defs.Look(ref incident, "incident");
            Scribe_Values.Look(ref pointsFactor, "pointsFactor", 1f);
            Scribe_Values.Look(ref outSignalRaw, "outSignalRaw");
            Scribe_Values.Look(ref outSignalScoped, "outSignalScoped");
            Scribe_Values.Look(ref forcedFactionDef, "forcedFactionDef");
            Scribe_Defs.Look(ref arrivalMode, "arrivalMode");
            Scribe_Defs.Look(ref raidStrategy, "raidStrategy");
        }
    }
}
