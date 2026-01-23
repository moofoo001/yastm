using HarmonyLib;
using Verse;
using RimWorld;

namespace YASTM
{
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "Projectile", MethodType.Getter)]
    public static class Patch_Verb_Projectile
    {
        public static void Postfix(Verb_LaunchProjectile __instance, ref ThingDef __result)
        {
            if (__instance == null || __instance.EquipmentSource == null) return;

            CompMultiModeWeapon comp = __instance.EquipmentSource.GetComp<CompMultiModeWeapon>();
            
            if (comp != null && comp.CurrentMode != null && comp.CurrentMode.projectileDef != null)
            {
                // WIR ÜBERSCHREIBEN DAS PROJEKTIL
                __result = comp.CurrentMode.projectileDef;

                // --- DIAGNOSE LOG ---
                // Nur loggen, wenn geschossen wird, sonst spammt es
                // Vergleiche diese ID (#...) mit der ID aus dem Button-Klick!
                if (Find.TickManager.TicksGame % 60 == 0) // Kleiner Spam-Schutz
                {
                     // Log.Message($"[YASTM PATCH] Reading Instance #{comp.GetHashCode()}. Current Index: {comp.CurrentMode.label}");
                }
            }
        }
    }
}