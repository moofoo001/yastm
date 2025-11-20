using System;
using Verse;
using RimWorld;
using RimWorld.QuestGen;

namespace StarTrekFactions.QuestNodes
{

    public class QuestNode_FindFarCells : QuestNode
    {
        public SlateRef<int> minDist = 40; 
        public SlateRef<int> maxDist = 70; 
        public SlateRef<bool> findTwo = true;

        protected override void RunInt()
        {
            var slate = QuestGen.slate;
            var map = slate.Get<Map>("map");
            if (map == null) return;

            IntVec3 a;
            if (TryFind(map, minDist.GetValue(slate), maxDist.GetValue(slate), out a))
                slate.Set("locA", a);

            if (findTwo.GetValue(slate))
            {
                IntVec3 b;

                if (TryFind(map, minDist.GetValue(slate), maxDist.GetValue(slate), out b, 18, a))
                    slate.Set("locB", b);
            }
        }

        protected override bool TestRunInt(Slate slate)
        {

            return slate.Get<Map>("map") != null;
        }

        private bool TryFind(Map map, int min, int max, out IntVec3 cell, int minFromOther = 0, IntVec3 other = default)
        {
            var center = map.Center;
            bool Validator(IntVec3 c)
            {
                if (!c.InBounds(map) || c.Fogged(map)) return false;
                if (!c.Standable(map)) return false;

                if (map.areaManager?.Home != null && map.areaManager.Home[c]) return false;

                int dist = (int) c.DistanceTo(center);
                if (dist < min || dist > max) return false;

                if (minFromOther > 0 && other.IsValid)
                {
                    if (c.DistanceTo(other) < minFromOther) return false;
                }

                var edifice = c.GetEdifice(map);
                if (edifice != null && edifice.def.passability == Traversability.Impassable) return false;

                return true;
            }


            return CellFinder.TryFindRandomCellNear(center, map, max, Validator, out cell);
        }
    }
}

