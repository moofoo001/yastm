using RimWorld;
using Verse;
using UnityEngine; 

namespace YASTM
{
    public class CompProperties_ForceFieldEmitter : CompProperties
    {
        public int effectRadius = 8;
        public int refreshTicks = 60;
        public string hediffDefName = "ST_Hediff_ForceFieldDrag";
        public string fleckDefName  = "ST_Fleck_ForceSpark";

        public CompProperties_ForceFieldEmitter()
        {
            compClass = typeof(CompForceFieldEmitter);
        }
    }

    public class CompForceFieldEmitter : ThingComp
    {
        private bool active;
        private int nextTick;

        public CompProperties_ForceFieldEmitter Props => (CompProperties_ForceFieldEmitter)props;

        // --- Settings-übersteuerbar ---
        public int EffectiveRadius =>
            (YASTM_Mod.Settings?.forceFieldRadius ?? Props.effectRadius) > 0
                ? (YASTM_Mod.Settings?.forceFieldRadius ?? Props.effectRadius)
                : 8;

        public int EffectiveRefresh =>
            (YASTM_Mod.Settings?.forceFieldRefreshTicks ?? Props.refreshTicks) > 0
                ? (YASTM_Mod.Settings?.forceFieldRefreshTicks ?? Props.refreshTicks)
                : 60;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            MapComponent_ForceFieldSystem.Register(this);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            MapComponent_ForceFieldSystem.Unregister(this, previousMap);
            base.PostDestroy(mode, previousMap);
        }

        public void SetActive(bool on, ThingComp controller)
        {
            active = on;
            MapComponent_ForceFieldSystem.MarkDirty(parent.Map);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!active) return;

            int tick = Find.TickManager.TicksGame;
            if (tick < nextTick) return;
            nextTick = tick + EffectiveRefresh;

            MapComponent_ForceFieldSystem.QueuePulse(parent.Map, this);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(parent.Position, EffectiveRadius);
        }

        public bool Active => active;
    }
}

