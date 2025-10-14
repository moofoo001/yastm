using System;
using Verse;
using RimWorld;
using RimWorld.QuestGen;

namespace StarTrekFactions.QuestNodes
{
    // Findet 1–2 Map-Zellen, die:
    // - standable & unreserved sind
    // - NICHT in der Home-Zone liegen
    // - in einem Distanzbereich zum Map-Center liegen
    // Ergebnisse werden als Slate-Keys (locA, locB) abgelegt.
    public class QuestNode_FindFarCells : QuestNode
    {
        public SlateRef<int> minDist = 40;     // Kacheln ab Map-Center
        public SlateRef<int> maxDist = 70;     // Kacheln ab Map-Center
        public SlateRef<bool> findTwo = true;  // zweiten Punkt suchen?

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
                // Suche b mit kleinem Mindestabstand zu a, damit sie nicht nebeneinander liegen
                if (TryFind(map, minDist.GetValue(slate), maxDist.GetValue(slate), out b, 18, a))
                    slate.Set("locB", b);
            }
        }

        protected override bool TestRunInt(Slate slate)
        {
            // Für TestRun reicht uns: map vorhanden?
            return slate.Get<Map>("map") != null;
        }

        private bool TryFind(Map map, int min, int max, out IntVec3 cell, int minFromOther = 0, IntVec3 other = default)
        {
            var center = map.Center;
            bool Validator(IntVec3 c)
            {
                if (!c.InBounds(map) || c.Fogged(map)) return false;
                if (!c.Standable(map)) return false;
                // nicht in Home-Zone
                if (map.areaManager?.Home != null && map.areaManager.Home[c]) return false;

                int dist = (int) c.DistanceTo(center);
                if (dist < min || dist > max) return false;

                if (minFromOther > 0 && other.IsValid)
                {
                    if (c.DistanceTo(other) < minFromOther) return false;
                }
                // optional: etwas „sichtbar“, nicht im dichten Gebüsch
                var edifice = c.GetEdifice(map);
                if (edifice != null && edifice.def.passability == Traversability.Impassable) return false;

                return true;
            }

            // breit suchen: von Center nach außen
            return CellFinder.TryFindRandomCellNear(center, map, max, Validator, out cell);
        }
    }
}
