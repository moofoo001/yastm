using System;
using System.Linq;
using RimWorld;
using Verse;

namespace YASTM.MapSystems
{
    /// <summary>
    /// Tracks comms cooldowns and provides helpers for labels + active quest count.
    /// </summary>
    public class MapComponent_CommsProgress : MapComponent
    {
        // Cooldowns (game ticks)
        private int nextAidAllowedTick;
        private int nextContactAllowedTick;

        public MapComponent_CommsProgress(Map map) : base(map) { }

        // Ready flags (properties — not methods)
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
            // Count ALL ongoing quests (any source).
            return Find.QuestManager.QuestsListForReading.Count(q => q.State == QuestState.Ongoing);
        }

        private static string FormatCooldown(int untilTick)
        {
            int now = Find.TickManager.TicksGame;
            int remaining = untilTick - now;
            if (remaining <= 0) return "Ready";
            // Use RimWorld's built-in formatter (handles days/hours/mins text)
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
