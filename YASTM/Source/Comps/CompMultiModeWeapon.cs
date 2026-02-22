using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

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
        public string iconPath;
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

        // HIER IST DER SAUBERE VANILLA WEG (Wird automatisch von RimWorld aufgerufen)
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = null;
            if (parent.ParentHolder is Pawn_EquipmentTracker eq) pawn = eq.pawn;
            else if (parent.ParentHolder is Pawn p) pawn = p;

            if (pawn != null && pawn.Faction == Faction.OfPlayer && CurrentMode != null)
            {
                yield return new Command_PhaserMode(this);
            }
        }
    }

    // 3. UNSER EIGENER, INTELLIGENTER BUTTON (Mit Linksklick & Rechtsklick!)
    public class Command_PhaserMode : Command_Action
    {
        private CompMultiModeWeapon comp;

        public Command_PhaserMode(CompMultiModeWeapon comp)
        {
            this.comp = comp;
            this.groupKey = 3133701 + comp.parent.def.shortHash;
            UpdateVisuals();
            
            // Was passiert beim Linksklick?
            this.action = delegate 
            {
                this.comp.CycleMode();
                UpdateVisuals(); // Bild sofort aktualisieren!
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

        // Was passiert beim Rechtsklick? (Das coole neue Menü!)
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