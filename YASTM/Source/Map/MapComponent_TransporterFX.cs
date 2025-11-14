using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class MapComponent_TransporterFX : MapComponent
    {
        private struct Entry
        {
            public Pawn pawn;
            public int ticksLeft;
            public int nextBurstIn;
        }

        private readonly List<Entry> active = new List<Entry>();

        public MapComponent_TransporterFX(Map map) : base(map) { }

        public void StartRematerialize(Pawn pawn, int durationTicks = 180)
        {
            if (pawn == null || pawn.Map != map) return;

            // Pawn ~3s „verweilen“ lassen
            pawn.stances.stunner.StunFor(durationTicks, null, addBattleLog: false, showMote: false);

            // Sofort ein erster Burst
            SpawnBurst(pawn);

            active.Add(new Entry
            {
                pawn = pawn,
                ticksLeft = durationTicks,
                nextBurstIn = 12 // alle 12 Ticks ≈ 0.2s
            });
        }

        public override void MapComponentTick()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                e.ticksLeft--;
                e.nextBurstIn--;

                if (e.nextBurstIn <= 0 && e.pawn != null && e.pawn.Map == map)
                {
                    SpawnBurst(e.pawn);
                    // Nächster Burst
                    e.nextBurstIn = 12; // Timing feinjustierbar
                }

                if (e.ticksLeft <= 0 || e.pawn == null || e.pawn.DestroyedOrNull())
                {
                    active.RemoveAt(i);
                }
                else
                {
                    active[i] = e;
                }
            }
        }

        private static FleckDef FleckAttached =>
            DefDatabase<FleckDef>.GetNamedSilentFail("ST_Fleck_TransporterColumn_Attached");

        // Ein Burst = 3 überlappende Overlays (leicht versetzt & skaliert)
        private void SpawnBurst(Pawn pawn)
        {
            if (FleckAttached == null || pawn == null || pawn.Map == null) return;

            // kleine Versatz-Offsets für „übereinander wobbeln“
            var o1 = new Vector3(0f, 0f, 0f);
            var o2 = new Vector3(0.06f, 0f, 0.02f);
            var o3 = new Vector3(-0.05f, 0f, -0.02f);

            FleckMaker.AttachedOverlay(pawn, FleckAttached, o1, 1.10f);
            FleckMaker.AttachedOverlay(pawn, FleckAttached, o2, 1.00f);
            FleckMaker.AttachedOverlay(pawn, FleckAttached, o3, 0.95f);
        }
    }
}
