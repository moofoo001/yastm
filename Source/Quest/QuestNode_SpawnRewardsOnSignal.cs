using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_SpawnRewardsOnSignal : QuestNode
    {
        public string inSignal;
        public bool dropPod = true;
        public List<RewardEntry> rewards = new List<RewardEntry>();

        protected override void RunInt()
        {
            var p = new QuestPart_SpawnRewardsOnSignal
            {
                inSignalRaw    = inSignal,
                inSignalScoped = QuestGenUtility.HardcodedSignalWithQuestID(inSignal),
                dropPod        = dropPod,
                rewards        = rewards
            };
            QuestGen.quest.AddPart(p);
        }

        protected override bool TestRunInt(Slate slate)
            => !inSignal.NullOrEmpty() && rewards != null && rewards.Count > 0;
    }

    public class RewardEntry : IExposable
    {
        public ThingDef thingDef;
        public int count = 1;
        public ThingDef stuff;                 
        public QualityCategory? quality = null;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look(ref count, "count", 1);
            Scribe_Defs.Look(ref stuff, "stuff");
            Scribe_Values.Look(ref quality, "quality", null);
        }
    }

    public class QuestPart_SpawnRewardsOnSignal : QuestPart
    {
        public string inSignalRaw, inSignalScoped;
        public bool dropPod = true;
        public List<RewardEntry> rewards = new List<RewardEntry>();
        public bool fired;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (fired) return;
            if (signal.tag != inSignalRaw && signal.tag != inSignalScoped) return;
            fired = true;

            Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null) return;

            var things = new List<Thing>();
            foreach (var r in rewards)
            {
                if (r?.thingDef == null || r.count <= 0) continue;
                var t = ThingMaker.MakeThing(r.thingDef, r.stuff);
                if (r.quality.HasValue) t.TryGetComp<CompQuality>()?.SetQuality(r.quality.Value, ArtGenerationContext.Outsider);
                t.stackCount = r.count;
                things.Add(t);
            }

            if (things.Count == 0) return;

            IntVec3 drop = DropCellFinder.TradeDropSpot(map);
            if (dropPod)
                DropPodUtility.DropThingsNear(drop, map, things, canRoofPunch: false, leaveSlag: false, forbid: true);
            else
                foreach (var t in things)
                    GenPlace.TryPlaceThing(t, drop, map, ThingPlaceMode.Near);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Values.Look(ref dropPod, "dropPod", true);
            Scribe_Collections.Look(ref rewards, "rewards", LookMode.Deep);
            Scribe_Values.Look(ref fired, "fired", false);
        }
    }
}
