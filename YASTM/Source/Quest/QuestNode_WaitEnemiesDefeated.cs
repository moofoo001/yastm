using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_WaitEnemiesDefeated : QuestNode
    {
        public string inSignal;
        public string inSignalRaw;
        public bool onlyManhunters = false;
        public string outSignal;

        protected override void RunInt()
        {
            string raw = inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw;
            var part = new QuestPart_WaitEnemiesDefeated
            {
                inSignalRaw     = raw,
                inSignalScoped  = QuestGenUtility.HardcodedSignalWithQuestID(raw),
                onlyManhunters  = onlyManhunters,
                outSignalRaw    = outSignal,
                outSignalScoped = outSignal.NullOrEmpty() ? null : QuestGenUtility.HardcodedSignalWithQuestID(outSignal)
            };
            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate)
            => !(inSignal.NullOrEmpty() && inSignalRaw.NullOrEmpty()) && !outSignal.NullOrEmpty();
    }

    public class QuestPart_WaitEnemiesDefeated : QuestPart
    {
        public string inSignalRaw, inSignalScoped;
        public bool onlyManhunters;
        public string outSignalRaw, outSignalScoped;

        private bool active;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == inSignalRaw || signal.tag == inSignalScoped)
            {
                active = true;
                StarTrekFactions.GameComponent_QuestWatchers.Instance?.Register(this);
            }
        }

        public bool CheckAndMaybeComplete()
        {
            if (!active) return false;
            Map map = Find.AnyPlayerHomeMap ?? Find.Maps.FirstOrDefault();
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
                return true;
            }
            return false;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Values.Look(ref onlyManhunters, "onlyManhunters");
            Scribe_Values.Look(ref outSignalRaw, "outSignalRaw");
            Scribe_Values.Look(ref outSignalScoped, "outSignalScoped");
            Scribe_Values.Look(ref active, "active");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && active)
                StarTrekFactions.GameComponent_QuestWatchers.Instance?.Register(this);
        }
    }
}
