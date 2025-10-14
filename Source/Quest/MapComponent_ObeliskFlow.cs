using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StarTrekFactions
{
    public class MapComponent_ObeliskFlow : MapComponent
    {
        // Status-Flags, auf die andere Comps zugreifen (Gating/Usability)
        public bool scanA;
        public bool scanB;
        public bool manhuntTriggered;

        public MapComponent_ObeliskFlow(Map map) : base(map) { }

        // NICHT readonly -> sonst CS0192 beim Scribe
        private List<WaitDirective> waits = new List<WaitDirective>();

        private class WaitDirective : IExposable
        {
            public int questId;
            public string outSignal = "STQ.Obelisks.II.Cleared";
            public bool onlyManhunters = true;
            public Thing subject;

            public void ExposeData()
            {
                Scribe_Values.Look(ref questId, "questId", 0);
                Scribe_Values.Look(ref outSignal, "outSignal", "STQ.Obelisks.II.Cleared");
                Scribe_Values.Look(ref onlyManhunters, "onlyManhunters", true);
                Scribe_References.Look(ref subject, "subject");
            }
        }

        // Vom QuestPart scharfgestellt: warte, bis keine Bedrohung mehr aktiv ist
        public void StartWaitEnemiesDefeated(int questId, string outSignal, Thing subject, bool onlyManhunters)
        {
            waits.Add(new WaitDirective
            {
                questId        = questId,
                outSignal      = outSignal ?? "STQ.Obelisks.II.Cleared",
                onlyManhunters = onlyManhunters,
                subject        = subject
            });
        }

        public override void MapComponentTick()
        {
            if (waits.Count == 0) return;

            for (int i = waits.Count - 1; i >= 0; i--)
            {
                var w = waits[i];

                bool anyThreat;
                if (w.onlyManhunters)
                {
                    anyThreat = map.mapPawns.AllPawnsSpawned.Any(p =>
                        p.Spawned && !p.Downed && p.RaceProps.Animal &&
                        (p.MentalStateDef == MentalStateDefOf.Manhunter ||
                         p.MentalStateDef == MentalStateDefOf.ManhunterPermanent));
                }
                else
                {
                    anyThreat = GenHostility.AnyHostileActiveThreatToPlayer(map);
                }

                if (anyThreat) continue;

                // Alles sauber -> "Cleared" (roh + quest-scoped) senden
                var raw  = w.outSignal ?? "STQ.Obelisks.II.Cleared";
                var args = new SignalArgs(w.subject?.Named("SUBJECT") ?? map.Named("SUBJECT"));

                Find.SignalManager.SendSignal(new Signal(raw, args));
                Find.SignalManager.SendSignal(new Signal($"Quest{w.questId}.{raw}", args));
                Log.Message($"[YASTM][WAIT] cleared -> sent '{raw}' (+scoped) for quest {w.questId} on map '{map}'");

                waits.RemoveAt(i);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref scanA, "scanA", false);
            Scribe_Values.Look(ref scanB, "scanB", false);
            Scribe_Values.Look(ref manhuntTriggered, "manhuntTriggered", false);
            Scribe_Collections.Look(ref waits, "waits", LookMode.Deep);
        }

        // Kompatibilität: alter Fallback ruft das evtl. noch; macht jetzt nur Marker, kein Raid
        public void TriggerManhuntOnce(Map targetMap)
        {
            if (manhuntTriggered) return;
            manhuntTriggered = true;
            Log.Message("[YASTM] TriggerManhuntOnce() called (noop — Quest steuert den Raid).");
        }
    }
}
