// Source/Comps/CompObeliskHighlight.cs
using RimWorld;
using Verse;
using Verse.Sound;

namespace StarTrekFactions
{
    public class CompProperties_ObeliskHighlight : CompProperties
    {
        public FleckDef pulseFleck;                
        public string  pulseFleckDefName = "ST_ObeliskPulse"; 
        public SoundDef humSound;                   
        public int   pulseInterval = 180;
        public float pulseRadius   = 3.2f;

        public CompProperties_ObeliskHighlight() { compClass = typeof(CompObeliskHighlight); }
    }

    public class CompObeliskHighlight : ThingComp
    {
        private Sustainer sust;
        private int nextPulse;
        private FleckDef cachedFleck;

        private CompProperties_ObeliskHighlight Props => (CompProperties_ObeliskHighlight)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            nextPulse = Find.TickManager.TicksGame + Rand.Range(30, Props.pulseInterval);
        }

        public override void CompTick()
        {
            if (parent?.Map == null) return;

            
            if (Props.humSound != null)
            {
                bool onScreen = Find.CameraDriver.CurrentViewRect.Contains(parent.Position);
                if (onScreen)
                {
                    if (sust == null || sust.Ended)
                        sust = Props.humSound.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTick));
                    sust?.Maintain();
                }
                else if (sust != null)
                {
                    sust.End();
                    sust = null;
                }
            }

            
            int now = Find.TickManager.TicksGame;
            if (now < nextPulse) return;
            nextPulse = now + Props.pulseInterval;

            var fleck = ResolveFleck();
            if (fleck == null) return;

            FleckMaker.Static(parent.TrueCenter(), parent.Map, fleck, Props.pulseRadius);
        }

        private FleckDef ResolveFleck()
        {
            if (cachedFleck != null) return cachedFleck;
            cachedFleck = Props.pulseFleck;
            if (cachedFleck == null && !Props.pulseFleckDefName.NullOrEmpty())
                cachedFleck = DefDatabase<FleckDef>.GetNamedSilentFail(Props.pulseFleckDefName);
            if (cachedFleck == null)
                cachedFleck = DefDatabase<FleckDef>.GetNamedSilentFail("PsycastAreaEffect") ?? FleckDefOf.DustPuffThick;
            return cachedFleck;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (sust != null) { sust.End(); sust = null; }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            GenDraw.DrawRadiusRing(parent.Position, Props.pulseRadius);
        }
    }
}

