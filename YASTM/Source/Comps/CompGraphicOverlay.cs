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
                // Versuche, die Referenzen aufzulösen (wichtig für Comps!)
                Props.graphicData.ResolveReferencesSpecial();
                
                // Grafik erstellen
                overlayGraphic = Props.graphicData.GraphicColoredFor(parent);
                
                if (overlayGraphic == null || overlayGraphic.MatSingle == null)
                {
                    Log.Warning($"[YASTM] Overlay graphic failed to load for {parent.def.defName}. Path: {Props.graphicData.texPath}");
                }
                else
                {
                    // Einmalige Bestätigung im Log (nur beim ersten Mal)
                    Log.Message($"[YASTM] Overlay graphic loaded successfully for {parent.def.defName}");
                }
            }
        }

        public override void PostDraw()
        {
            // Sicherheits-Check: Falls beim Spawn nicht geladen wurde (passiert manchmal)
            if (overlayGraphic == null) TryLoadGraphic();
            if (overlayGraphic == null) return;

            // 1. Position holen
            Vector3 drawPos = parent.DrawPos;

            // 2. Absolute Höhe erzwingen (MetaOverlays ist sehr hoch)
            // Wir addieren +2.0f, um sicher über ALLEM zu sein (Dächer, Nebel, UI-Marker)
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 2.0f;
            
            // 3. Offset anwenden (rotiert mit dem Gebäude)
            Vector3 finalOffset = Props.offset;
            // Falls das Gebäude gedreht ist, muss der Offset mitgedreht werden
            finalOffset = finalOffset.RotatedBy(parent.Rotation);
            drawPos += finalOffset;

            // 4. Zeichnen
            // Wir nutzen parent.Rotation, damit das Overlay sich mitdreht
            overlayGraphic.Draw(drawPos, parent.Rotation, parent);
        }
    }
}