using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ST.Start
{
    // MapComponent läuft auf jeder neuen Map; verschiebt Kolonisten einmalig ins Shuttle.
    public class MapComponent_ShuttleStart : MapComponent
    {
        private bool moved;

        // Passe diese DefNames an deine Shuttle-Def(s) an
        private static readonly string[] ShuttleDefNames = new[]
        {
            "ST_StartShuttle",          // bevorzugter Name
            "ST_Shuttle_Start",
            "ST_Structure_ShuttleStart"
        };

        public MapComponent_ShuttleStart(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            if (moved) return;
            // Warte bis Kolonisten wirklich gespawnt sind (kein GameInitData-Gefrickel)
            var pawns = map.mapPawns?.FreeColonistsSpawned?.ToList();
            if (pawns == null || pawns.Count == 0) return;

            var shuttle = FindShuttle(map);
            if (shuttle == null)
            {
                // Falls kein Shuttle gefunden: lieber nichts tun.
                moved = true;
                return;
            }

            var targetCells = GetInteriorCells(shuttle, map).ToList();
            if (targetCells.Count == 0)
            {
                // Fallback: neben dem Shuttle
                targetCells = GenAdj.CellsAdjacentCardinal(shuttle).Where(c => c.Standable(map)).ToList();
            }
            if (targetCells.Count == 0)
            {
                moved = true;
                return;
            }

            // Kolonisten ins Shuttle bringen (ein Pawn pro Zelle)
            int i = 0;
            foreach (var pawn in pawns)
            {
                if (i >= targetCells.Count) break;
                var cell = targetCells[i++];
                // sanft stoppen & versetzen
                pawn.jobs?.StopAll();
                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, cell, map);
                pawn.pather?.StopDead();
            }

            moved = true;
        }

        private Thing FindShuttle(Map map)
        {
            foreach (var defName in ShuttleDefNames)
            {
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null) continue;
                var t = map.listerThings.ThingsOfDef(def).FirstOrDefault();
                if (t != null) return t;
            }
            return null;
        }

        private IEnumerable<IntVec3> GetInteriorCells(Thing shuttle, Map map)
        {
            // Nimmt die belegte Rechteckfläche des Shuttles und filtert auf begehbar + roofed
            var rect = GenAdj.OccupiedRect(shuttle.Position, shuttle.Rotation, shuttle.def.size);
            foreach (var c in rect)
            {
                if (!c.InBounds(map)) continue;
                if (c.Standable(map) && c.Roofed(map) && !c.Filled(map))
                    yield return c;
            }
        }
    }
}
