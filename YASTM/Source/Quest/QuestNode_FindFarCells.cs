using System;
using Verse;
using RimWorld;
using RimWorld.QuestGen;


namespace YASTM.Source.Quest
{
    public class QuestNode_FindFarCells : QuestNode
    {
        public SlateRef<int> minDist = 40; 
        public SlateRef<int> maxDist = 70; 
        public SlateRef<bool> findTwo = true;

        protected override void RunInt()
        {
            // FIX: Verse.Map verwenden (hattest du hier schon richtig)
            Verse.Map map = Find.AnyPlayerHomeMap; 
            if (map == null) return;

            Slate slate = QuestGen.slate;
            IntVec3 a;
            
            // Aufruf unserer Hilfsmethode
            if (TryFind(map, minDist.GetValue(slate), maxDist.GetValue(slate), out a))
                slate.Set("locA", a);

            if (findTwo.GetValue(slate))
            {
                IntVec3 b;
                // Aufruf Hilfsmethode
                if (TryFind(map, minDist.GetValue(slate), maxDist.GetValue(slate), out b, 18, a))
                    slate.Set("locB", b);
            }
        }
        
        protected override bool TestRunInt(Slate slate) => true;

        // FIX: Hier lag der Fehler! "Map" -> "Verse.Map"
        private bool TryFind(Verse.Map map, int min, int max, out IntVec3 cell, int minFromOther = 0, IntVec3 other = default)
        {
            var center = map.Center;
            
            // Lokale Validierungs-Funktion
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