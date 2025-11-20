using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    public class GameComponent_RankPipSync : GameComponent
    {
        private bool didSync;

        public GameComponent_RankPipSync(Game game) {}

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref didSync, "YASTM_didRankPipSync", false);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (didSync) return;
            if (Find.TickManager.TicksGame < 1) return;           // warte 1 Tick bis alle Pawns gespawnt sind
            if (Current.Game?.Maps == null || Current.Game.Maps.Count == 0) return;

            SyncAllPlayerPawns();
            didSync = true;
        }

        private void SyncAllPlayerPawns()
        {
            var pawns = PawnsFinder.AllMaps_FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
                TryEnsurePipsMatchRank(pawns[i]);
        }

        private void TryEnsurePipsMatchRank(Pawn pawn)
        {
            if (pawn?.apparel == null || pawn.story?.traits == null) return;

            // Finde den ersten ST_Rank_* Trait (falls mehrere, nimm den neuesten/obersten)
            Trait rankTrait = null;
            var all = pawn.story.traits.allTraits;
            for (int i = 0; i < all.Count; i++)
            {
                var tr = all[i];
                if (tr?.def?.defName != null && tr.def.defName.StartsWith("ST_Rank_"))
                    rankTrait = tr;
            }
            if (rankTrait == null) return;

            // Alte Pips runter
            var worn = pawn.apparel.WornApparel;
            for (int i = worn.Count - 1; i >= 0; i--)
            {
                var ap = worn[i];
                if (ap?.def?.defName != null && ap.def.defName.StartsWith("ST_RankPips_"))
                {
                    pawn.apparel.Remove(ap);
                    ap.Destroy(DestroyMode.Vanish);
                }
            }

            // Neue Pips gemäß RankVisualExtension
            var ext = rankTrait.def.GetModExtension<RankVisualExtension>();
            if (ext == null || string.IsNullOrEmpty(ext.pipApparelDefName)) return;

            var pipDef = DefDatabase<ThingDef>.GetNamedSilentFail(ext.pipApparelDefName);
            if (pipDef == null) return;

            var pip = ThingMaker.MakeThing(pipDef) as Apparel;
            if (pip == null) return;

            pawn.apparel.Wear(pip, dropReplacedApparel: true);
        }
    }
}

