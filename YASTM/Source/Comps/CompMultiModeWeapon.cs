using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib; // NEU: Damit wir RimWorld zwingen können, den Button zu zeigen!

namespace YASTM
{
    // 1. DIE XML-EINSTELLUNGEN
    public class CompProperties_MultiModeWeapon : CompProperties
    {
        public List<WeaponModeDef> modes = new List<WeaponModeDef>();
        public CompProperties_MultiModeWeapon() { this.compClass = typeof(CompMultiModeWeapon); }
    }

    public class WeaponModeDef
    {
        public string label;
        public string iconPath; // HIER WIRD DER PFAD AUS DER XML GELESEN!
        public ThingDef projectileDef;
        public SoundDef soundInteract;
        public bool isOverload = false;
        public float overloadSelfExplodeChance = 0.05f; 
    }

    // 2. DAS HERZSTÜCK
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

        // Diese Methode baut den Button (mit dem Icon aus der XML)
        public IEnumerable<Gizmo> GetEquippedGizmos()
        {
            if (CurrentMode != null)
            {
                Command_Action switchMode = new Command_Action();
                switchMode.defaultLabel = CurrentMode.label;
                switchMode.defaultDesc = $"Cycle weapon mode.\nCurrent: {CurrentMode.label}";
                
                if (!CurrentMode.iconPath.NullOrEmpty())
                    switchMode.icon = ContentFinder<Texture2D>.Get(CurrentMode.iconPath, false);
                if (switchMode.icon == null) switchMode.icon = TexCommand.Attack; 

                switchMode.action = delegate 
                { 
                    currentModeIndex++;
                    if (currentModeIndex >= Props.modes.Count) currentModeIndex = 0;

                    if (CurrentMode.soundInteract != null)
                        CurrentMode.soundInteract.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
                };
                switchMode.activateSound = SoundDefOf.Click;

                yield return switchMode;
            }
        }
    }

    // =====================================================================
    // 3. DER FEHLENDE PATCH: Zwingt RimWorld, den Button in der Leiste zu zeigen!
    // =====================================================================
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_PhaserGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            // Zeige zuerst alle normalen Buttons (Bewegen, Schießen etc.)
            foreach (var gizmo in values) yield return gizmo;

            // Wenn es ein Spieler-Kolonist ist und er eine Waffe hält...
            if (__instance.Faction == Faction.OfPlayer && __instance.equipment?.Primary != null)
            {
                // ...prüfe ob es unser Phaser ist!
                var comp = __instance.equipment.Primary.GetComp<CompMultiModeWeapon>();
                if (comp != null)
                {
                    // Zeige unseren Button an!
                    foreach (var customGizmo in comp.GetEquippedGizmos())
                    {
                        yield return customGizmo;
                    }
                }
            }
        }
    }

    // 4. DAS VERB (Wie die Waffe feuert)
    public class Verb_PhaserShoot : Verb_Shoot
    {
        public override ThingDef Projectile
        {
            get
            {
                if (EquipmentSource == null) return base.Projectile;
                var comp = EquipmentSource.GetComp<CompMultiModeWeapon>();
                
                if (comp != null && comp.CurrentMode != null && comp.CurrentMode.projectileDef != null)
                {
                    return comp.CurrentMode.projectileDef;
                }
                return base.Projectile;
            }
        }

        protected override bool TryCastShot()
        {
            if (EquipmentSource == null) return base.TryCastShot();
            var comp = EquipmentSource.GetComp<CompMultiModeWeapon>();
            
            if (comp != null && comp.CurrentMode != null && comp.CurrentMode.isOverload)
            {
                if (Rand.Chance(comp.CurrentMode.overloadSelfExplodeChance))
                {
                    Pawn pawn = CasterPawn;
                    if (pawn != null)
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Overload Misfire!", 3f);
                        GenExplosion.DoExplosion(pawn.Position, pawn.Map, 1.9f, DamageDefOf.Bomb, pawn, 10, weapon: EquipmentSource.def);
                        return false; 
                    }
                }
            }
            return base.TryCastShot();
        }
    }
}