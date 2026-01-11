using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM.Source.Comps
{
    public class CompProperties_GraphicOverlay : CompProperties
    {
        public GraphicData graphicData;
        public Vector3 offset = Vector3.zero;
        public AltitudeLayer altitude = AltitudeLayer.MetaOverlays; 

        public CompProperties_GraphicOverlay()
        {
            this.compClass = typeof(CompGraphicOverlay);
        }
    }

    public class CompGraphicOverlay : ThingComp
    {
        public CompProperties_GraphicOverlay Props => (CompProperties_GraphicOverlay)props;
        private Graphic overlayGraphic;
        private bool textureLoadAttempted = false;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            TryLoadGraphic();
        }

        private void TryLoadGraphic()
        {
            if (textureLoadAttempted) return;
            textureLoadAttempted = true;

            if (Props.graphicData != null)
            {
                // resolve references
                Props.graphicData.ResolveReferencesSpecial();
                
                // load graphic
                overlayGraphic = Props.graphicData.GraphicColoredFor(parent);
                
                if (overlayGraphic == null || overlayGraphic.MatSingle == null)
                {
                    Log.Warning($"[YASTM] Overlay graphic failed to load for {parent.def.defName}. Path: {Props.graphicData.texPath}");
                }
                else
                {
                
                    Log.Message($"[YASTM] Overlay graphic loaded successfully for {parent.def.defName}");
                }
            }
        }

        public override void PostDraw()
        {
            // assert graphic loaded
            if (overlayGraphic == null) TryLoadGraphic();
            if (overlayGraphic == null) return;

            Vector3 drawPos = parent.DrawPos;

            // set altitude
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 2.0f;
            
            // apply offset
            Vector3 finalOffset = Props.offset;
            finalOffset = finalOffset.RotatedBy(parent.Rotation);
            drawPos += finalOffset;

            // draw overlay
            overlayGraphic.Draw(drawPos, parent.Rotation, parent);
        }
    }
}