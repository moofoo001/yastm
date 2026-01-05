using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    // Die Definitionen aus dem XML
    public class CompProperties_Holodeck : CompProperties
    {
        public float trainingXpPerTick = 0.15f;
        public float injuryChance = 0.001f;
        
        // Angepasst an deine Namenswünsche:
        public float powerTrainingMode = 2000f; // Hoher Verbrauch (Combat/Training)
        public float powerRelaxMode = 400f;     // Niedriger Verbrauch (Risa/Joy)

        public CompProperties_Holodeck()
        {
            this.compClass = typeof(CompHolodeck);
        }
    }

    // WICHTIG: StaticConstructorOnStartup wird benötigt, um Texturen/Materialien zu laden
    [StaticConstructorOnStartup] 
    public class CompHolodeck : ThingComp
    {
        public CompProperties_Holodeck Props => (CompProperties_Holodeck)this.props;

        // --- VISUALS ---
        // Wir laden die Grafiken direkt hier. Pfade müssen exakt stimmen!
        // Stelle sicher, dass diese Dateien in 'Textures/Things/Building/Misc/' liegen.
        private static readonly Material MatRisa = MaterialPool.MatFrom("Things/Building/Misc/RisaOverlay", ShaderDatabase.Transparent);
        private static readonly Material MatCombat = MaterialPool.MatFrom("Things/Building/Misc/TrainingOverlay", ShaderDatabase.Transparent);

        private CompPowerTrader powerComp;
        
        // Status-Variablen
        private bool isRunningProgram = false;
        private bool isTrainingMode = false; // true = Combat, false = Risa
        
        // Für die Animation (Pulsieren)
        private float animPulse = 0f;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = this.parent.GetComp<CompPowerTrader>();
        }

        public override void CompTick()
        {
            base.CompTick();

            // 1. Strom-Check
            if (powerComp != null && !powerComp.PowerOn)
            {
                ResetState();
                return;
            }

            // 2. Nutzer-Erkennung (Wer steht drauf?)
            Pawn user = GetUserAtInteractionCell();

            if (user != null)
            {
                // A) TRAINING (Rezept/Bill wird abgearbeitet)
                // JobDefOf.DoBill ist der Standard-Job für Werkbänke
                if (user.CurJobDef == JobDefOf.DoBill)
                {
                    isRunningProgram = true;
                    isTrainingMode = true; 
                    powerComp.PowerOutput = -Props.powerTrainingMode; // 2000 W
                }
                // B) RISA (Freizeit/Joy)
                // Prüft, ob der Job zur Kategorie Erholung gehört
                else if (user.CurJob.def.joyKind != null) 
                {
                    isRunningProgram = true;
                    isTrainingMode = false;
                    powerComp.PowerOutput = -Props.powerRelaxMode; // 400 W
                }
                else
                {
                    ResetState(); // Pawn steht nur rum (z.B. Drafted)
                }
            }
            else
            {
                ResetState();
            }

            // 3. Animation weiterschalten
            if (isRunningProgram)
            {
                // Lässt den Wert langsam von 0.0 bis 1.0 laufen und resettet dann
                animPulse += 0.008f; 
                if (animPulse > 1f) animPulse = 0f;
            }
            else
            {
                animPulse = 0f;
            }
        }

        private void ResetState()
        {
            isRunningProgram = false;
            isTrainingMode = false;
            // Setzt Stromverbrauch auf den Standardwert aus dem <basePowerConsumption> Tag im XML zurück
            if (powerComp != null) powerComp.SetUpPowerVars(); 
        }

        // --- GRAFIK RENDERING (Ersetzt die alte Gizmo-Logik) ---
        public override void PostDraw()
        {
            base.PostDraw();

            if (!isRunningProgram) return; // Nichts zeichnen, wenn aus

            // Wähle das Material basierend auf dem Modus
            Material matToDraw = isTrainingMode ? MatCombat : MatRisa;

            if (matToDraw != null)
            {
                // Animation: Basisgröße (2.5) + Puls (bis zu +0.5)
                float size = 2.5f + (animPulse * 0.5f);
                
                // Matrix für Position und Größe
                Vector3 s = new Vector3(size, 1f, size);
                Matrix4x4 matrix = default(Matrix4x4);
                
                // Position: Zentriert, aber etwas über dem Boden ("MoteOverhead" Layer)
                Vector3 pos = this.parent.TrueCenter();
                pos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead); 
                
                matrix.SetTRS(pos, Quaternion.AngleAxis(0, Vector3.up), s);

                // Zeichnet das Bild in die Welt
                Graphics.DrawMesh(MeshPool.plane10, matrix, matToDraw, 0);
            }
        }

        // Hilfsmethode: Findet den aktiven Nutzer
        private Pawn GetUserAtInteractionCell()
        {
            if (!parent.Spawned) return null;
            
            IntVec3 cell = parent.InteractionCell;
            List<Thing> thingList = cell.GetThingList(parent.Map);
            
            foreach (Thing t in thingList)
            {
                if (t is Pawn p && !p.Dead && !p.Downed)
                {
                    // Optional: Prüfen, ob der Pawn wirklich UNS benutzt
                    if (p.CurJob != null && p.CurJob.targetA.Thing == parent)
                    {
                        return p;
                    }
                }
            }
            return null;
        }
    }
}