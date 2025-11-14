using RimWorld;
using Verse;

namespace YASTM
{
    public class MapComponent_CommsProgress : MapComponent
    {
        public int nextAidAllowedTick;

        public MapComponent_CommsProgress(Map map) : base(map) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextAidAllowedTick, "nextAidAllowedTick", 0);
        }

        public bool AidReadyNow => Find.TickManager.TicksGame >= nextAidAllowedTick;

        public int CooldownTicksLeft => nextAidAllowedTick - Find.TickManager.TicksGame;
    }
}
