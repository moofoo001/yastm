using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Patch_OverloadRisk
    {
        public static bool Prefix(Verb_LaunchProjectile __instance, ref bool __result)
        {
            // weapon has comp
            if (__instance.EquipmentSource == null) return true;
            
            var comp = __instance.EquipmentSource.GetComp<CompMultiModeWeapon>();
            if (comp == null || comp.CurrentMode == null) return true;

            // is set to overload
            if (comp.CurrentMode.isOverload)
            {
                // roll chance
                if (Rand.Chance(comp.CurrentMode.overloadSelfExplodeChance))
                {
                    Pawn shooter = __instance.CasterPawn;
                    
                    if (shooter != null)
                    {
                        Log.Message($"[YASTM] {shooter.Name} Phaser OVERLOAD DETONATION!");
                        Messages.Message($"{shooter.LabelShort}'s weapon overloaded and exploded!", shooter, MessageTypeDefOf.NegativeEvent);

                        // do explosion
                        GenExplosion.DoExplosion(
                            center: shooter.Position, 
                            map: shooter.Map, 
                            radius: 1.9f, 
                            damType: DamageDefOf.Bomb, 
                            instigator: shooter, 
                            damAmount: 15,
                            armorPenetration: -1f,
                            explosionSound: null,
                            weapon: __instance.EquipmentSource.def, 
                            projectile: null, 
                            intendedTarget: null
                        );

                        // destroy weapon
                        __instance.EquipmentSource.Destroy();
                    }

                    // cancel shot
                    __result = false;
                    return false; 
                }
            }

            return true;
        }
    }
}