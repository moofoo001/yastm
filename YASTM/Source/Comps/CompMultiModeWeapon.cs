using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    // 1. DIE EINSTELLUNGEN
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

    // 2. DAS HERZSTÜCK (Der Comp & Button)
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

        // NEU: Idiotensichere Methode, um den Träger der Waffe zu finden, 
        // egal wie tief RimWorld die Waffe im Inventar verschachtelt hat!
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

        // HIER IST DER FIX: Wir nutzen GetPawnOwner()
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = GetPawnOwner();
            
            // Zeige den Button nur, wenn die Waffe einem Spieler-Kolonisten gehört
            if (pawn == null || pawn.Faction != Faction.OfPlayer) yield break;

            if (CurrentMode != null)
            {
                Command_Action switchMode = new Command_Action();
                switchMode.defaultLabel = CurrentMode.label;
                switchMode.defaultDesc = $"Cycle weapon mode.\nCurrent: {CurrentMode.label}";
                
                if (!CurrentMode.iconPath.NullOrEmpty())
                    switchMode.icon = ContentFinder<Texture2D>.Get(CurrentMode.iconPath, false);
                if (switchMode.icon == null) switchMode.icon = TexCommand.Attack; 

                switchMode.action = delegate { CycleMode(pawn); };
                switchMode.activateSound = SoundDefOf.Click;

                yield return switchMode;
            }
        }

        private void CycleMode(Pawn pawn)
        {
            currentModeIndex++;
            if (currentModeIndex >= Props.modes.Count) currentModeIndex = 0;

            if (CurrentMode.soundInteract != null)
                CurrentMode.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, CurrentMode.label, 2f);
        }
    }

    // 3. DAS VERB (Wie die Waffe feuert)
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