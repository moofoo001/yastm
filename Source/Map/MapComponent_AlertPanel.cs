using RimWorld;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class MapComponent_AlertPanel : MapComponent
    {
        // RED
        public int NextAllowedTick;
        public int BlinkUntilTick;
        public Sustainer activeSiren;

        // YELLOW
        public int NextAllowedTickYellow;
        public int BlinkUntilTickYellow;
        public Sustainer activeSirenYellow;

        // Hediff-Defs (lazy per Tick aufgelöst)
        static HediffDef HediffRed    => DefDatabase<HediffDef>.GetNamedSilentFail("ST_Alert_RedState");
        static HediffDef HediffYellow => DefDatabase<HediffDef>.GetNamedSilentFail("ST_Alert_YellowState");

        public MapComponent_AlertPanel(Map map) : base(map) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref NextAllowedTick,       "YASTM_RedAlert_NextAllowedTick", 0);
            Scribe_Values.Look(ref BlinkUntilTick,        "YASTM_RedAlert_BlinkUntilTick",  0);
            Scribe_Values.Look(ref NextAllowedTickYellow, "YASTM_YellowAlert_NextAllowedTick", 0);
            Scribe_Values.Look(ref BlinkUntilTickYellow,  "YASTM_YellowAlert_BlinkUntilTick",  0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            int now = Find.TickManager.TicksGame;

            // Sustain die Sirenen solange Blink aktiv ist
            if (activeSiren != null)
            {
                activeSiren.Maintain();
                if (now >= BlinkUntilTick) { activeSiren.End(); activeSiren = null; }
            }
            if (activeSirenYellow != null)
            {
                activeSirenYellow.Maintain();
                if (now >= BlinkUntilTickYellow) { activeSirenYellow.End(); activeSirenYellow = null; }
            }

            // Alle 250 Ticks: Hediffs je nach aktivem Alert-Status setzen/entfernen
            if (now % 250 == 0)
            {
                bool redActive    = now < BlinkUntilTick;
                bool yellowActive = now < BlinkUntilTickYellow;

                ApplyAlertHediffs(redActive, yellowActive);
            }

            // Alle 90 Ticks: Blinker/Effekte rendern
            if (now % 90 != 0) return;

            var panelDef = DefDatabase<ThingDef>.GetNamedSilentFail("ST_AlertPanel");
            if (panelDef == null) return;

            var panels = map.listerThings.ThingsOfDef(panelDef);
            if (panels == null || panels.Count == 0) return;

            if (now < BlinkUntilTick)
            {
                var fleckRed = DefDatabase<FleckDef>.GetNamedSilentFail("ST_AlertBlink_Red") ?? FleckDefOf.Smoke;
                foreach (var t in panels) FleckMaker.Static(t.Position, map, fleckRed, 1.2f);
            }
            if (now < BlinkUntilTickYellow)
            {
                var fleckYellow = DefDatabase<FleckDef>.GetNamedSilentFail("ST_AlertBlink_Yellow") ?? FleckDefOf.Smoke;
                foreach (var t in panels) FleckMaker.Static(t.Position, map, fleckYellow, 1.1f);
            }
        }

        // -------------------------------------------------
        // Hediff-Management
        // -------------------------------------------------
        void ApplyAlertHediffs(bool redActive, bool yellowActive)
        {
            var pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                var p = pawns[i];
                if (p?.health == null) continue;

                // Alt entfernen
                TryRemove(p, HediffRed);
                TryRemove(p, HediffYellow);

                // Neu setzen (exklusiv)
                if (redActive && HediffRed != null)
                    p.health.AddHediff(HediffRed);
                else if (yellowActive && HediffYellow != null)
                    p.health.AddHediff(HediffYellow);
            }
        }

        static void TryRemove(Pawn p, HediffDef def)
        {
            if (def == null) return;
            var h = p.health.hediffSet.GetFirstHediffOfDef(def);
            if (h != null) p.health.RemoveHediff(h);
        }
    }
}
