// YASTM comp multi mode weapon v1.8.3


using System.Collections.Generic;  
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib; // WICHTIG: Damit erzwingen wir den Button!

namespace YASTM
{
    public class CompProperties_MultiModeWeapon : CompProperties
    {
        public List<WeaponModeDef> modes = new List<WeaponModeDef>();
        public CompProperties_MultiModeWeapon() { this.compClass = typeof(CompMultiModeWeapon); }
    }

    public class WeaponModeDef
    {
        public string label;
        public string iconPath; // Hier wird das Bild aus der XML gezogen!
        public ThingDef projectileDef;
        public SoundDef soundInteract;
        public bool isOverload = false;
        public float overloadSelfExplodeChance = 0.05f; 
    }

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

        public void CycleMode()
        {
            currentModeIndex++;
            if (currentModeIndex >= Props.modes.Count) currentModeIndex = 0;
            if (CurrentMode?.soundInteract != null)
                CurrentMode.soundInteract.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        }

        public void SetMode(int index)
        {
            currentModeIndex = index;
            if (CurrentMode?.soundInteract != null)
                CurrentMode.soundInteract.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        }

        // Diese Methode wird jetzt sicher von unserem Patch aufgerufen!
        public IEnumerable<Gizmo> GetPhaserGizmos()
        {
            if (CurrentMode != null)
            {
                yield return new Command_PhaserMode(this);
            }
        }
    }

    public class Command_PhaserMode : Command_Action
    {
        private CompMultiModeWeapon comp;

        public Command_PhaserMode(CompMultiModeWeapon comp)
        {
            this.comp = comp;
            this.groupKey = 3133701 + comp.parent.def.shortHash;
            UpdateVisuals();
            
            this.action = delegate 
            {
                this.comp.CycleMode();
                UpdateVisuals();
            };
        }

        private void UpdateVisuals()
        {
            this.defaultLabel = comp.CurrentMode.label;
            this.defaultDesc = $"Left-click to cycle.\nRight-click for list.\nCurrent: {comp.CurrentMode.label}";
            
            if (!comp.CurrentMode.iconPath.NullOrEmpty())
                this.icon = ContentFinder<Texture2D>.Get(comp.CurrentMode.iconPath, false);
            else
                this.icon = TexCommand.Attack;
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                for (int i = 0; i < comp.Props.modes.Count; i++)
                {
                    int index = i;
                    list.Add(new FloatMenuOption(comp.Props.modes[i].label, delegate
                    {
                        comp.SetMode(index);
                        UpdateVisuals();
                    }));
                }
                return list;
            }
        }
    }

    // =================================================================
    // DER ABSOLUT KUGELSICHERE PATCH
    // Zwingt RimWorld, den Button JEDEM Kolonisten zu geben, der die Waffe hält!
    // =================================================================
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetPhaserGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var gizmo in values) yield return gizmo;

            if (__instance.Faction != Faction.OfPlayer) yield break;

            if (__instance.equipment != null && __instance.equipment.Primary != null)
            {
                var comp = __instance.equipment.Primary.GetComp<CompMultiModeWeapon>();
                if (comp != null)
                {
                    foreach (var gizmo in comp.GetPhaserGizmos())
                    {
                        yield return gizmo;
                    }
                }
            }
        }
    }

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