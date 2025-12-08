using System;
using System.Linq;
using RimWorld;
using Verse;

namespace YASTM.MapSystems
{

    public class MapComponent_CommsProgress : MapComponent
    {
 
        private int nextAidAllowedTick;
        private int nextContactAllowedTick;

        public MapComponent_CommsProgress(Map map) : base(map) { }


        public bool AidReady => Find.TickManager.TicksGame >= nextAidAllowedTick;
        public bool ContactReady => Find.TickManager.TicksGame >= nextContactAllowedTick;
        public void RegisterScan(bool atA)
        {
            var flow = map.GetComponent<YASTM.MapSystems.MapComponent_ObeliskFlow>();
            flow?.RegisterScan(atA);
        }
        public void ArmAidCooldown(float days)
        {
            int add = (int)Math.Ceiling(days * GenDate.TicksPerDay);
            nextAidAllowedTick = Find.TickManager.TicksGame + add;
        }

        public void ArmContactCooldown(float days)
        {
            int add = (int)Math.Ceiling(days * GenDate.TicksPerDay);
            nextContactAllowedTick = Find.TickManager.TicksGame + add;
        }

        public string AidCooldownLabel(Map _)      => FormatCooldown(nextAidAllowedTick);
        public string ContactCooldownLabel(Map _)  => FormatCooldown(nextContactAllowedTick);

        public int ActiveQuestCount()
        {

            return Find.QuestManager.QuestsListForReading.Count(q => q.State == QuestState.Ongoing);
        }

        private static string FormatCooldown(int untilTick)
        {
            int now = Find.TickManager.TicksGame;
            int remaining = untilTick - now;
            if (remaining <= 0) return "Ready";

            return remaining.ToStringTicksToPeriod();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextAidAllowedTick, "nextAidAllowedTick", 0);
            Scribe_Values.Look(ref nextContactAllowedTick, "nextContactAllowedTick", 0);
        }
    }
}
