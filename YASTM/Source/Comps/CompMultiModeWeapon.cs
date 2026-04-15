// YASTM CompMultiModeWeapon v1.8.5
using System.Collections.Generic;  
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib;

namespace YASTM
{
    // =================================================================
    // VERSIONS-TRACKER 
    // =================================================================
    [StaticConstructorOnStartup]
    public static class YASTM_MultiModeInit
    {
        static YASTM_MultiModeInit()
        {
            //Log.Message("[YASTM DEBUG] Initialized CompMultiModeWeapon v1.8.5");
        }
    }

    // 1. settings
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

    // 2. the core
    public class CompMultiModeWeapon : ThingComp
    {
        public CompProperties_MultiModeWeapon Props => (CompProperties_MultiModeWeapon)props;
        private int currentModeIndex = 0;
        
        // flicker fix  
        private Command_Action cachedGizmo;

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

        private Pawn GetPawnOwner()
        {
            IThingHolder holder = parent.ParentHolder;
            while (holder != null)
            {
                if (holder is Pawn_EquipmentTracker eq) return eq.pawn;
                if (holder is Pawn p) return p;
                holder = holder.ParentHolder;
            }
            return null;
        }

        // This method is called by the Harmony patch below!
        public IEnumerable<Gizmo> GetPhaserGizmos()
        {
            if (CurrentMode == null) yield break;

            if (cachedGizmo == null)
            {
                cachedGizmo = new Command_Action();
                cachedGizmo.groupKey = 3133701 + parent.def.shortHash; 
            }

            cachedGizmo.defaultLabel = CurrentMode.label;
            cachedGizmo.defaultDesc = $"Click to cycle weapon mode.\nCurrent: {CurrentMode.label}";
            
            if (!CurrentMode.iconPath.NullOrEmpty())
                cachedGizmo.icon = ContentFinder<Texture2D>.Get(CurrentMode.iconPath, false);
            else
                cachedGizmo.icon = TexCommand.Attack; 

            cachedGizmo.action = delegate 
            { 
                CycleMode(); 
            };
            
            cachedGizmo.activateSound = SoundDefOf.Click;

            yield return cachedGizmo;
        }

        private void CycleMode()
        {
            currentModeIndex++;
            if (currentModeIndex >= Props.modes.Count) currentModeIndex = 0;

            Pawn pawn = GetPawnOwner();
            if (pawn != null)
            {
                if (CurrentMode.soundInteract != null)
                    CurrentMode.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, CurrentMode.label, 2f);
            }
        }
    }

    // =================================================================
    // 3. PATCH
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

    // 4. THE VERB
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