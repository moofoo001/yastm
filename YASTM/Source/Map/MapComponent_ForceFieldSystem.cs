using System.Collections.Generic;
using System.Linq;                    // <<--- LINQ für Where/OrderBy
using Verse;
using RimWorld;

namespace YASTM
{
    /// <summary>
    /// Wendet periodisch den Hediff auf Pawns im Radius aktiver Emitter an
    /// und erzeugt optional Rand-Flecks.
    /// </summary>
    public class MapComponent_ForceFieldSystem : MapComponent
    {
        private static readonly Dictionary<Map, HashSet<CompForceFieldEmitter>> ActiveEmitters = new();
        private static readonly Dictionary<Map, List<CompForceFieldEmitter>> PulseQueue = new();

        public MapComponent_ForceFieldSystem(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            if (!PulseQueue.TryGetValue(map, out var list) || list.Count == 0) return;

            var work = new List<CompForceFieldEmitter>(list);
            list.Clear();

            for (int i = 0; i < work.Count; i++)
            {
                var em = work[i];
                if (em == null || em.parent == null || em.parent.Destroyed || !em.Active) continue;
                ApplyAura(em);
            }
        }

        private void ApplyAura(CompForceFieldEmitter em)
        {
            var center = em.parent.Position;
            int radius = em.EffectiveRadius;
            var hediff = DefDatabase<HediffDef>.GetNamedSilentFail(em.Props.hediffDefName);
            var fleck  = DefDatabase<FleckDef>.GetNamedSilentFail(em.Props.fleckDefName);

            bool hostilesOnly = YASTM_Mod.Settings?.forceFieldHostilesOnly ?? false;

            foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                var pawn = cell.GetFirstPawn(map);
                if (pawn == null || pawn.Dead) continue;
                if (hostilesOnly && !pawn.HostileTo(Faction.OfPlayer)) continue;

                if (hediff != null && pawn.health?.hediffSet != null)
                {
                    var h = pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
                    if (h == null)
                    {
                        h = HediffMaker.MakeHediff(hediff, pawn);
                        pawn.health.AddHediff(h);
                    }
                    if (h.TryGetComp<HediffComp_Disappears>() is { } disp)
                        disp.ticksToDisappear = Rand.RangeInclusive(60, 90);
                }

                if (fleck != null && Rand.Chance(0.06f))
                    FleckMaker.Static(cell, map, fleck, 0.8f);
            }

            // Rand-Schimmer: 3-5 kurze Wellen direkt am Radius
            var ripple = DefDatabase<FleckDef>.GetNamedSilentFail("ST_Fleck_FieldRipple");
            if (ripple != null)
            {
                var edge = GenRadial.RadialCellsAround(center, radius, true)
                    .Where(c => c.InBounds(map) && c.DistanceTo(center) >= radius - 0.6f)
                    .ToList();

                int count = Rand.RangeInclusive(3, 5);
                for (int i = 0; i < count && edge.Count > 0; i++)
                {
                    var cell = edge[Rand.RangeInclusive(0, edge.Count - 1)];
                    FleckMaker.Static(cell, map, ripple, 1.0f);
                }
            }
        }

        // ---- Static API für Comps ----
        public static void Register(CompForceFieldEmitter em)
        {
            var m = em.parent.Map;
            if (m == null) return;
            if (!ActiveEmitters.TryGetValue(m, out var set))
            {
                set = new HashSet<CompForceFieldEmitter>();
                ActiveEmitters[m] = set;
            }
            set.Add(em);
        }

        public static void Unregister(CompForceFieldEmitter em, Map previousMap)
        {
            var m = previousMap ?? em.parent?.Map;
            if (m == null) return;
            if (ActiveEmitters.TryGetValue(m, out var set))
                set.Remove(em);
        }

        public static void MarkDirty(Map map) { /* Reserve for caches */ }

        public static void QueuePulse(Map map, CompForceFieldEmitter em)
        {
            if (map == null) return;
            if (!PulseQueue.TryGetValue(map, out var l))
            {
                l = new List<CompForceFieldEmitter>();
                PulseQueue[map] = l;
            }
            l.Add(em);
        }
    }
}
