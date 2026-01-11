using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using YASTM.Source.Systems; // Für GameComponent_QuestWatchers

namespace YASTM.Source.Quest
{
    public class QuestPart_WaitEnemiesDefeated : QuestPart
    {
        // added fields
        public string inSignalRaw;
        public string inSignalScoped;
        public bool onlyManhunters;
        public string outSignalRaw;
        public string outSignalScoped;

        private bool active;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == inSignalRaw || signal.tag == inSignalScoped)
            {
                active = true;
                // register with the quest watchers
                GameComponent_QuestWatchers.Instance?.Register(this);
            }
        }

        // method called by the quest watcher each tick
        public bool CheckAndMaybeComplete()
        {
            if (!active) return false;
            
            Verse.Map map = Find.AnyPlayerHomeMap ?? Find.Maps.FirstOrDefault();
            if (map == null) return false;

            bool hostilesRemain = map.mapPawns.AllPawnsSpawned.Any(p =>
                p.Spawned && !p.Dead && p.HostileTo(Faction.OfPlayer) &&
                (!onlyManhunters || (p.RaceProps.Animal &&
                 (p.MentalStateDef == MentalStateDefOf.Manhunter || p.MentalStateDef == MentalStateDefOf.ManhunterPermanent)))
            );

            if (!hostilesRemain)
            {
                active = false;
                if (!outSignalRaw.NullOrEmpty())    Find.SignalManager.SendSignal(new Signal(outSignalRaw));
                if (!outSignalScoped.NullOrEmpty()) Find.SignalManager.SendSignal(new Signal(outSignalScoped));
                return true; // completed
            }
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Values.Look(ref onlyManhunters, "onlyManhunters");
            Scribe_Values.Look(ref outSignalRaw, "outSignalRaw");
            Scribe_Values.Look(ref outSignalScoped, "outSignalScoped");
            Scribe_Values.Look(ref active, "active");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && active)
                GameComponent_QuestWatchers.Instance?.Register(this);
        }
    }
}