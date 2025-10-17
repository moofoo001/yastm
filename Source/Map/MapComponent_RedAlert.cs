using RimWorld;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class MapComponent_RedAlert : MapComponent
    {
        public int NextAllowedTick;
        public int BlinkUntilTick;

        // Laufende, loopende Sirene (optional)
        public Sustainer activeSiren;

        public MapComponent_RedAlert(Map map) : base(map) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref NextAllowedTick, "YASTM_RedAlert_NextAllowedTick", 0);
            Scribe_Values.Look(ref BlinkUntilTick, "YASTM_RedAlert_BlinkUntilTick", 0);
            // activeSiren nicht speichern (nur temporär)
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            int now = Find.TickManager.TicksGame;

            // Sustainer am Leben halten und ggf. beenden
            if (activeSiren != null)
            {
                activeSiren.Maintain();
                if (now >= BlinkUntilTick)
                {
                    activeSiren.End();
                    activeSiren = null;
                }
            }

            // Blink-Pulse
            if (now >= BlinkUntilTick) return;
            if (now % 90 != 0) return;

            var fleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_RedAlertBlink") ?? FleckDefOf.Smoke;
            var panelDef = DefDatabase<ThingDef>.GetNamedSilentFail("ST_RedAlertPanel");
            if (panelDef == null) return;

            var panels = map.listerThings.ThingsOfDef(panelDef);
            if (panels == null) return;

            foreach (var t in panels)
                FleckMaker.Static(t.Position, map, fleck, 1.2f);
        }
    }
}
