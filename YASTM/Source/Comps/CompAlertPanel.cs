using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    // Die Definition "public enum AlertLevel" wurde hier ENTFERNT, 
    // um den Konflikt zu lösen. Wir nutzen jetzt das globale "ST_AlertLevel".

    public class CompProperties_AlertPanel : CompProperties
    {
        // Diese Variablen bleiben erhalten, damit Ihre XML-Dateien keine Fehler werfen
        public int redDurationTicks = 18000;
        public int yellowDurationTicks = 9000;
        public int redCooldownTicks = 90000;
        public int yellowCooldownTicks = 45000;

        public CompProperties_AlertPanel()
        {
            this.compClass = typeof(CompAlertPanel);
        }
    }

    public class CompAlertPanel : ThingComp
    {
        // Zugriff auf das globale Alert-System der Karte
        private ST_AlertLevel CurrentLevel
        {
            get
            {
                if (parent.Map == null) return ST_AlertLevel.Normal;
                
                var system = parent.Map.GetComponent<ST_ColonyAlertSystem>();
                return system != null ? system.CurrentLevel : ST_AlertLevel.Normal;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            
            // Status prüfen
            ST_AlertLevel level = CurrentLevel;

            // Wenn Alarm ist (Gelb oder Rot), zeichnen wir ein Overlay
            if (level != ST_AlertLevel.Normal)
            {
                // 1. Farbe wählen
                Color color = (level == ST_AlertLevel.Red) ? Color.red : Color.yellow;
                
                // 2. Pulsieren berechnen (Sinus-Welle)
                float pulseSpeed = (level == ST_AlertLevel.Red) ? 5f : 3f; // Rot blinkt schneller
                float num = (Time.realtimeSinceStartup * pulseSpeed) % 6.28f; 
                float alpha = 0.4f + (Mathf.Sin(num) * 0.3f); // Transparenz zwischen 0.1 und 0.7
                color.a = alpha;

                // 3. Matrix für das Zeichnen erstellen (Position & Größe)
                Vector3 drawPos = parent.DrawPos;
                drawPos.y += 0.04f; // Zeichne es leicht ÜBER dem Gebäude, damit man es sieht
                
                Vector3 size = new Vector3(parent.def.graphicData.drawSize.x, 1f, parent.def.graphicData.drawSize.y);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, parent.Rotation.AsQuat, size);

                // 4. Das farbige Overlay zeichnen
                // Wir nutzen ein einfaches "Plane"-Mesh und färben es ein.
                // Das spart extra Texturen und sieht modern aus.
                Graphics.DrawMesh(MeshPool.plane10, matrix, SolidColorMaterials.SimpleSolidColorMaterial(color), 0);
            }
        }
    }
}