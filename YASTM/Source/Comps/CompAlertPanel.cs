using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class CompProperties_AlertPanel : CompProperties
    {
        public int redDurationTicks = 2500;
        public int redCooldownTicks = 60000;
        public int yellowDurationTicks = 5000;
        public int yellowCooldownTicks = 30000;
        public int blinkSeconds = 2; 

        public CompProperties_AlertPanel()
        {
            this.compClass = typeof(CompAlertPanel);
        }
    }

    [StaticConstructorOnStartup]
    public class CompAlertPanel : ThingComp
    {
        private static readonly Material MatGreen = MaterialPool.MatFrom("Things/Building/OpsSecurity/AlertPanel_Green", ShaderDatabase.MetaOverlay);
        private static readonly Material MatYellow = MaterialPool.MatFrom("Things/Building/OpsSecurity/AlertPanel_Yellow", ShaderDatabase.MetaOverlay);
        private static readonly Material MatRed = MaterialPool.MatFrom("Things/Building/OpsSecurity/AlertPanel_Red", ShaderDatabase.MetaOverlay);

        // FIX: Neuer Klassenname hier verwendet
        private MapComponent_ColonyAlert mapComp;
        public CompProperties_AlertPanel Props => (CompProperties_AlertPanel)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent.Map != null)
            {
                // FIX: Neuer Klassenname
                mapComp = parent.Map.GetComponent<MapComponent_ColonyAlert>();
                mapComp?.Register(this);
            }
        }

        public void UpdateVisuals()
        {
            if (parent.Spawned && parent.Map != null)
            {
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (mapComp == null) return;

            Material matToDraw = null;
            switch (mapComp.CurrentLevel)
            {
                case AlertLevel.Green: matToDraw = MatGreen; break;
                case AlertLevel.Yellow: matToDraw = MatYellow; break;
                case AlertLevel.Red: matToDraw = MatRed; break;
            }

            if (matToDraw != null)
            {
                Vector3 s = new Vector3(parent.def.graphicData.drawSize.x, 1f, parent.def.graphicData.drawSize.y);
                Matrix4x4 matrix = default(Matrix4x4);
                Vector3 pos = parent.DrawPos + new Vector3(0, 0.01f, 0); 
                matrix.SetTRS(pos, parent.Rotation.AsQuat, s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, matToDraw, 0);
            }
        }
    }
}