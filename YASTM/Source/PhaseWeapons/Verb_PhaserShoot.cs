using RimWorld;
using Verse;
using YASTM; // Make sure this matches your Comp's namespace!

namespace YASTM
{
    public class Verb_PhaserShoot : Verb_Shoot
    {
        // This overrides the standard projectile selection
        public override ThingDef Projectile
        {
            get
            {
                if (EquipmentSource == null) return base.Projectile;

                // This will now find the ONLY comp (since we fixed XML)
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
            
            // Overload Risk Logic
            if (comp != null && comp.CurrentMode != null && comp.CurrentMode.isOverload)
            {
                if (Rand.Chance(comp.CurrentMode.overloadSelfExplodeChance))
                {
                    Pawn pawn = CasterPawn;
                    if (pawn != null)
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Overload Misfire!", 3f);
                        Log.Warning($"[YASTM] {pawn.LabelShort}'s phaser exploded!");
                        
                        GenExplosion.DoExplosion(
                            center: pawn.Position, 
                            map: pawn.Map, 
                            radius: 1.9f, 
                            damType: DamageDefOf.Bomb, 
                            instigator: pawn,
                            damAmount: 10,
                            weapon: EquipmentSource.def
                        );
                        
                        return false; 
                    }
                }
            }

            return base.TryCastShot();
        }
    }
}