using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class CompMultiModeWeapon : ThingComp
    {
        public CompProperties_MultiModeWeapon Props => (CompProperties_MultiModeWeapon)props;
        private int currentModeIndex = 0;

        public WeaponModeDef CurrentMode 
        {
            get
            {
                if (Props.modes.NullOrEmpty()) return null;
                if (currentModeIndex >= Props.modes.Count) currentModeIndex = 0;
                return Props.modes[currentModeIndex];
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentModeIndex, "currentModeIndex", 0);
        }

        public IEnumerable<Gizmo> GetWeaponGizmos()
        {
            Pawn pawn = GetPawnOwner();
            if (pawn == null || !pawn.IsColonistPlayerControlled) yield break;

            if (CurrentMode != null)
            {
                Command_Action switchMode = new Command_Action();
                switchMode.defaultLabel = CurrentMode.label;
                switchMode.defaultDesc = $"Cycle weapon mode.\nCurrent: {CurrentMode.label}";
                
                // --- ICON LOGIK MIT DEBUGGING ---
                Texture2D iconTex = null;
                
                if (!CurrentMode.iconPath.NullOrEmpty())
                {
                    // Versuche das Icon zu laden
                    iconTex = ContentFinder<Texture2D>.Get(CurrentMode.iconPath, false);
                    
                    // DEBUG: Schreib ins Log, wenn Textur fehlt!
                    if (iconTex == null)
                    {
                        // Nur einmal warnen, um Spam zu vermeiden (optional)
                        Log.Warning($"[YASTM] FEHLENDES ICON: Konnte '{CurrentMode.iconPath}' nicht finden!");
                    }
                }

                // Wenn gefunden, setzen. Wenn nicht, nimm das Schwert als Warnung.
                if (iconTex != null)
                    switchMode.icon = iconTex;
                else
                    switchMode.icon = TexCommand.Attack; // Schwert = Bild fehlt!

                switchMode.action = delegate { CycleMode(pawn); };
                switchMode.activateSound = SoundDefOf.Click;

                yield return switchMode;
            }
        }
        
        // Alte Methode leer lassen, da wir jetzt über Harmony patchen
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break; 
        }

        private void CycleMode(Pawn pawn)
        {
            currentModeIndex++;
            if (currentModeIndex >= Props.modes.Count) currentModeIndex = 0;

            if (CurrentMode.soundInteract != null)
                CurrentMode.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, CurrentMode.label, 2f);
            
            // Debug Log, damit wir sehen, was passiert
            Log.Message($"[YASTM] Switch Mode -> Label: {CurrentMode.label} | IconPath: {CurrentMode.iconPath}");
        }

        private Pawn GetPawnOwner()
        {
            if (parent.ParentHolder is Pawn_EquipmentTracker tracker) return tracker.pawn;
            if (parent.ParentHolder is Pawn p) return p;
            return null;
        }
    }
    
    // ... (Hier müssen wieder die Properties und Def Klassen stehen, wie zuvor) ...
    public class CompProperties_MultiModeWeapon : CompProperties
    {
        public List<WeaponModeDef> modes = new List<WeaponModeDef>();
        public CompProperties_MultiModeWeapon() { this.compClass = typeof(CompMultiModeWeapon); }
    }

    public class WeaponModeDef
    {
        public string label;
        public string iconPath;
        public ThingDef projectileDef;
        public SoundDef soundInteract;
        // NEUES FELD: Ist das ein gefährlicher Modus?
        public bool isOverload = false;
        // Chance für Selbstzerstörung (0.05 = 5%)
        public float overloadSelfExplodeChance = 0.05f;
    }
    
}