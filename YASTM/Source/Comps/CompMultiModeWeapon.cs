using System.Collections.Generic;
using System.Linq; // WICHTIG für Count()
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

        // Wir prüfen beim Start, ob etwas faul ist
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            
            // SECURITY CHECK: Hat die Waffe diesen Comp versehentlich doppelt?
            if (parent != null)
            {
                var duplicates = parent.GetComps<CompMultiModeWeapon>().ToList();
                if (duplicates.Count > 1)
                {
                    Log.Error($"[YASTM CRITICAL] WEAPON CONFIG ERROR: {parent.Label} has {duplicates.Count} COPIES of CompMultiModeWeapon! The code will confuse them.");
                }
            }
        }

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
                
                // ICON LOGIK
                if (!CurrentMode.iconPath.NullOrEmpty())
                    switchMode.icon = ContentFinder<Texture2D>.Get(CurrentMode.iconPath, false);
                
                if (switchMode.icon == null) switchMode.icon = TexCommand.Attack; 

                switchMode.action = delegate { CycleMode(pawn); };
                switchMode.activateSound = SoundDefOf.Click;

                yield return switchMode;
            }
        }
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break; 
        }

        private void CycleMode(Pawn pawn)
        {
            // Index hochzählen
            currentModeIndex++;
            if (currentModeIndex >= Props.modes.Count) currentModeIndex = 0;

            if (CurrentMode.soundInteract != null)
                CurrentMode.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, CurrentMode.label, 2f);
            
            // --- DIAGNOSE LOG ---
            // Wir loggen die ID dieser Instanz (GetHashCode).
            // Wenn diese ID anders ist als die im Patch (siehe unten), haben wir den Übeltäter.
            Log.Warning($"[YASTM BUTTON] Switched Instance #{this.GetHashCode()} to Index {currentModeIndex} ({CurrentMode.label})");
        }

        private Pawn GetPawnOwner()
        {
            if (parent.ParentHolder is Pawn_EquipmentTracker tracker) return tracker.pawn;
            if (parent.ParentHolder is Pawn p) return p;
            return null;
        }
    }
    
    // Properties und Defs Klassen müssen hier bleiben...
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
        public bool isOverload = false;
        public float overloadSelfExplodeChance = 0.05f; 
    }
}