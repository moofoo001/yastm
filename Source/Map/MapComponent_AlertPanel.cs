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

        public MapComponent_AlertPanel(Map map) : base(map) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref NextAllowedTick,        "YASTM_RedAlert_NextAllowedTick", 0);
            Scribe_Values.Look(ref BlinkUntilTick,         "YASTM_RedAlert_BlinkUntilTick",  0);
            Scribe_Values.Look(ref NextAllowedTickYellow,  "YASTM_YellowAlert_NextAllowedTick", 0);
            Scribe_Values.Look(ref BlinkUntilTickYellow,   "YASTM_YellowAlert_BlinkUntilTick",  0);
            // Sustainer nicht saven
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            int now = Find.TickManager.TicksGame;

            // Sustainer maintain/stop
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

            // Blink alle ~1.5s
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
    }
}
