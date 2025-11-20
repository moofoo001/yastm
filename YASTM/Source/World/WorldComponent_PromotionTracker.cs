using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;   // <- Wichtig: World & WorldComponent
using Verse;

namespace YASTM
{
    public class WorldComponent_PromotionTracker : WorldComponent
    {
        private Dictionary<int, int> lastPromotionTickByPawn = new Dictionary<int, int>();

        public WorldComponent_PromotionTracker(World world) : base(world) {}

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref lastPromotionTickByPawn,
                "YASTM_lastPromotionTickByPawn", LookMode.Value, LookMode.Value);
        }

        public void RecordPromotion(Pawn pawn)
        {
            if (pawn == null) return;
            lastPromotionTickByPawn[pawn.thingIDNumber] = Find.TickManager.TicksGame;
        }

        public int TicksSinceLastPromotion(Pawn pawn)
        {
            if (pawn == null) return int.MaxValue;
            if (!lastPromotionTickByPawn.TryGetValue(pawn.thingIDNumber, out var last))
                return int.MaxValue;
            return Find.TickManager.TicksGame - last;
        }

        public static WorldComponent_PromotionTracker Get()
        {
            return Find.World.GetComponent<WorldComponent_PromotionTracker>();
        }
    }
}

