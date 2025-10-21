using RimWorld.Planet;  
using Verse;

namespace YASTM
{
    public class WorldComponent_StarfleetAid : WorldComponent
    {
        public int NextAllowedTick = 0;

        public WorldComponent_StarfleetAid(World world) : base(world) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref NextAllowedTick, "YASTM_AidCooldown_NextAllowedTick", 0);
        }
    }
}
