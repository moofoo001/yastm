using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace YASTM.Source.Quest
{
    public class QuestPart_RunIncidentOnSignal : QuestPart
    {
        // Diese Felder haben gefehlt:
        public string inSignalRaw;
        public string inSignalScoped;
        public string outSignalRaw;
        public string outSignalScoped;

        public IncidentDef incident;
        public float pointsFactor = 1f;

        public string forcedFactionDef;
        public PawnsArrivalModeDef arrivalMode;
        public RaidStrategyDef raidStrategy;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignalRaw && signal.tag != inSignalScoped) return;
            if (incident == null) return;

            // FIX: Verse.Map explizit nutzen
            Verse.Map map = Find.AnyPlayerHomeMap ?? Find.Maps.FirstOrDefault();
            if (map == null) return;

            var cat = incident.category ?? IncidentCategoryDefOf.ThreatSmall;
            var parms = StorytellerUtility.DefaultParmsNow(cat, map);
            parms.forced = true;

            if (pointsFactor != 1f && parms.points > 0f)
                parms.points *= pointsFactor;

            if (!forcedFactionDef.NullOrEmpty())
            {
                var facDef = DefDatabase<FactionDef>.GetNamedSilentFail(forcedFactionDef);
                if (facDef != null)
                {
                    Faction fac = Find.FactionManager.AllFactions
                        .FirstOrDefault(f => f.def == facDef && !f.IsPlayer);

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
                    if (fac != null) parms.faction = fac;
                }
            }

            if (arrivalMode != null) parms.raidArrivalMode = arrivalMode;
            if (raidStrategy != null) parms.raidStrategy = raidStrategy;

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