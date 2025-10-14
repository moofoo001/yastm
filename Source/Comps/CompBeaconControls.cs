using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.QuestGen; // Slate, QuestUtility

namespace StarTrekFactions.Comps
{
    public class CompProperties_BeaconControls : CompProperties
    {
        // INIT (nur UI; das eigentliche Spawnen macht CompUseEffect_SpawnFarAndSignal)
        public string initSignal;
        public List<SpawnEntry> spawnOnInit; // legacy, unbenutzt
        public class SpawnEntry { public ThingDef thingDef; public int count = 1; }

        // REPORT
        public string reportSignal;
        public bool fireIncidentOnReport = false;   // Standard: false (Raid kommt aus der Quest)
        public IncidentDef incidentDef;
        public float pointsFactor = 1f;
        public bool startPhaseIIIOnReport = false;  // Standard: false (Phase III kommt aus der Quest)

        // UX
        public string initLabelKey = "ST.InitializeBeacon";
        public string reportLabelKey = "ST.ReportToStarfleet";
        public bool requireScansForReport = true;

        public CompProperties_BeaconControls()
        {
            compClass = typeof(CompBeaconControls);
        }
    }

    public class CompBeaconControls : ThingComp
    {
        public CompProperties_BeaconControls Props => (CompProperties_BeaconControls)props;

        private bool initialized;
        private bool scanA;
        private bool scanB;
        private bool reported;

        private bool Powered
        {
            get
            {
                var p = parent.TryGetComp<CompPowerTrader>();
                return p == null || p.PowerOn;
            }
        }

        // Liest die Scan-Flags wahlweise intern ODER aus der MapComponent (wo CompScanWork sie setzt)
        private bool ScansAreComplete()
        {
            if (scanA && scanB) return true;
            var mc = parent.Map?.GetComponent<StarTrekFactions.MapComponent_ObeliskFlow>();
            return mc != null && mc.scanA && mc.scanB;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;

            // === Initialize ===
            {
                var cmd = new Command_Action
                {
                    defaultLabel = (Props.initLabelKey ?? "ST.InitializeBeacon").Translate(),
                    defaultDesc  = "ST.InitializeBeacon.Desc".Translate(),
                    icon = TexCommand.DesirePower,
                    action = () =>
                    {
                        if (!Powered)
                        {
                            Messages.Message("ST.MustBePowered".Translate(), parent, MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        DoInit();
                    }
                };
                if (initialized) cmd.Disable("Already initialized.");
                else if (!Powered) cmd.Disable("Requires power.");
                yield return cmd;
            }

            // === Report to Starfleet ===
            bool scansComplete = ScansAreComplete();
            if (!Props.requireScansForReport || scansComplete)
            {
                var cmd = new Command_Action
                {
                    defaultLabel = (Props.reportLabelKey ?? "ST.ReportToStarfleet").Translate(),
                    defaultDesc  = "ST.ReportToStarfleet.Desc".Translate(),
                    icon = TexCommand.Attack,
                    action = () =>
                    {
                        if (!Powered)
                        {
                            Messages.Message("ST.MustBePowered".Translate(), parent, MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        DoReport();
                    }
                };
                if (reported) cmd.Disable("Already transmitted.");
                yield return cmd;
            }
            else
            {
                var cmd = new Command_Action
                {
                    defaultLabel = (Props.reportLabelKey ?? "ST.ReportToStarfleet").Translate(),
                    defaultDesc  = "ST.ReportToStarfleet.Desc".Translate()
                };
                cmd.Disable("Scan both obelisks first.");
                yield return cmd;
            }
        }

        private void DoInit()
        {
            var map = parent.Map;
            if (map == null) return;

            // Einzige Init-Logik: unsere UseEffect (spawnt weit weg + sendet BeaconActivated + startet Setup-Quest)
            var use = parent.TryGetComp<CompUseEffect_SpawnFarAndSignal>();
            if (use != null)
            {
                use.DoEffect(null);
                initialized = true; // UI-Button ausblenden
                return;
            }

            Log.Warning("[YASTM][BEACON] CompUseEffect_SpawnFarAndSignal missing on beacon.");
        }

        private void DoReport()
        {
            var map = parent.Map;
            if (map == null) return;

            if (reported)
            {
                Messages.Message("ST.ReportSent".Translate(), parent, MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            // 1) Report-Signal: raw + quest-scoped broadcasten
            var raw = Props.reportSignal;
            if (!raw.NullOrEmpty())
            {
                var args = parent.Named("SUBJECT");
                Find.SignalManager.SendSignal(new Signal(raw, args));
                int scoped = 0;
                foreach (var q in Find.QuestManager.QuestsListForReading)
                {
                    Find.SignalManager.SendSignal(new Signal($"Quest{q.id}.{raw}", args));
                    scoped++;
                }
                Log.Message($"[YASTM][BEACON] report broadcast '{raw}' + quest-scoped to {scoped} quests.");
            }

                reported = true;
                Messages.Message("ST.ReportSent".Translate(), parent, MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>Obelisk meldet erfolgreichen Scan → Beacon merkt, welcher fertig ist.</summary>
        public void Notify_ObeliskScanned(Thing obelisk)
        {
            if (obelisk == null) return;
            if (obelisk.def == STFDefOf.ST_Obelisk_A) scanA = true;
            else if (obelisk.def == STFDefOf.ST_Obelisk_B) scanB = true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref initialized, "st_beacon_initialized", false);
            Scribe_Values.Look(ref scanA, "st_scanA", false);
            Scribe_Values.Look(ref scanB, "st_scanB", false);
            Scribe_Values.Look(ref reported, "st_report_done", false);
        }
    }
}
