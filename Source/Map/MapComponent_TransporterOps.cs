using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM
{
    /// <summary>
    /// Plant verzögerte Teleports nach Warmup ein:
    /// - Pawn bleibt während Warmup gestunnt (durch MapComponent_TransporterFX).
    /// - Nach Ablauf: DeSpawn -> Spawn am Zielpad -> Pad-VFX + Rematerialisieren.
    /// </summary>
    public class MapComponent_TransporterOps : MapComponent
    {
        private struct Pending
        {
            public Pawn pawn;
            public IntVec3 toCell;
            public int dueTick;
        }

        private readonly List<Pending> _queue = new List<Pending>();

        public MapComponent_TransporterOps(Map map) : base(map) { }

        /// <summary>Teleport in delayTicks Ticks an toCell planen.</summary>
        public void ScheduleTeleport(Pawn pawn, IntVec3 toCell, int delayTicks)
        {
            if (pawn == null || pawn.Map != map || !toCell.IsValid) return;
            _queue.Add(new Pending
            {
                pawn = pawn,
                toCell = toCell,
                dueTick = Find.TickManager.TicksGame + delayTicks
            });
        }

        public override void MapComponentTick()
        {
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                var p = _queue[i];
                if (Find.TickManager.TicksGame < p.dueTick) continue;

                var pawn = p.pawn;
                if (pawn == null || pawn.Destroyed) { _queue.RemoveAt(i); continue; }

                var map = pawn.Map ?? this.map;
                var toCell = p.toCell.IsValid ? p.toCell : pawn.Position;

                // Sicherheitszelle suchen
                var safeTo = CellFinder.StandableCellNear(toCell, map, 1);

                // DeSpawn -> Spawn am Ziel
                if (pawn.Spawned) pawn.DeSpawn();
                GenSpawn.Spawn(pawn, safeTo, map);

                // Ziel-VFX: Pad-Effekt + 3s Rematerialisieren
                TransporterVFX.PlayBeam(map, safeTo);
                TransporterVFX.BeginRematerialize(pawn, 180);

                _queue.RemoveAt(i);
            }
        }
    }
}
