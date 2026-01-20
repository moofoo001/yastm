using HarmonyLib;
using Verse;
using RimWorld;

namespace YASTM
{
    // Wir patchen den Getter der Property 'Projectile' in 'Verb_LaunchProjectile'
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "get_Projectile")]
    public static class Patch_Verb_Projectile
    {
        public static void Postfix(Verb_LaunchProjectile __instance, ref ThingDef __result)
        {
            // Sicherheitschecks
            if (__instance == null || __instance.EquipmentSource == null) return;

            // 1. Hat die Waffe unseren Comp?
            CompMultiModeWeapon comp = __instance.EquipmentSource.GetComp<CompMultiModeWeapon>();
            
            // 2. Wenn ja, und Modus aktiv -> Projektil überschreiben
            if (comp != null && comp.CurrentMode != null && comp.CurrentMode.projectileDef != null)
            {
                // LOG ZUR KONTROLLE (Kannst du später entfernen)
                // Log.Message($"[YASTM] Projectile Override: {__result.defName} -> {comp.CurrentMode.projectileDef.defName}");
                
                __result = comp.CurrentMode.projectileDef;
            }
        }
    }
}