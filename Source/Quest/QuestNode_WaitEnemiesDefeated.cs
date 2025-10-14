// File: Source/Quest/QuestNode_WaitEnemiesDefeated.cs
using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_WaitEnemiesDefeated : QuestNode
    {
        public string inSignal;          // Start-Marker
        public bool onlyManhunters = false;
        public string outSignal;         // z.B. "STQ.Obelisks.II.Cleared"

        protected override void RunInt()
        {
            var part = new QuestPart_WaitEnemiesDefeated
            {
                inSignal = inSignal,
                onlyManhunters = onlyManhunters,
                outSignal = outSignal
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate) => !inSignal.NullOrEmpty() && !outSignal.NullOrEmpty();
    }

    public class QuestPart_WaitEnemiesDefeated : QuestPart
    {
        public string inSignal;
        public bool onlyManhunters;
        public string outSignal;

        private bool active;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == inSignal)
            {
                active = true;
                StarTrekFactions.GameComponent_QuestWatchers.Instance?.Register(this);
            }
        }

        /// <summary>
        /// Vom GameComponent alle ~60 Ticks aufgerufen.
        /// Gibt true zurück, wenn die Bedingung erfüllt wurde und der Watcher entfernt werden soll.
        /// </summary>
        public bool CheckAndMaybeComplete()
        {
            if (!active) return false;

            Map map = Find.AnyPlayerHomeMap ?? Find.Maps.FirstOrDefault();
            if (map == null) return false;

            bool hostilesRemain = map.mapPawns.AllPawnsSpawned.Any(p =>
                p.Spawned && !p.Dead && p.HostileTo(Faction.OfPlayer) &&
                (!onlyManhunters || (p.RaceProps.Animal && (p.MentalStateDef == MentalStateDefOf.Manhunter || p.MentalStateDef == MentalStateDefOf.ManhunterPermanent)))
            );

            if (!hostilesRemain)
            {
                active = false;
                if (!outSignal.NullOrEmpty())
                    Find.SignalManager.SendSignal(new Signal(outSignal));
                return true; // watcher kann entfernt werden
            }

            return false;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref onlyManhunters, "onlyManhunters");
            Scribe_Values.Look(ref outSignal, "outSignal");
            Scribe_Values.Look(ref active, "active");

            // Nach Laden wieder registrieren, falls noch aktiv
            if (Scribe.mode == LoadSaveMode.PostLoadInit && active)
                StarTrekFactions.GameComponent_QuestWatchers.Instance?.Register(this);
        }
    }
}
