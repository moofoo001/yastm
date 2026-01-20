using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;

namespace YASTM
{
    // Wir patchen den Pawn direkt, um sicherzustellen, dass der Button erscheint
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_PawnGizmos
    {
        // Postfix mit 'IEnumerable' erlaubt uns, Dinge zur Liste hinzuzufügen
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            // 1. Erstmal alle normalen Buttons durchlassen (Draft, Magic, Psycasts...)
            foreach (var g in values)
            {
                yield return g;
            }

            // 2. Prüfen: Hat der Pawn Equipment?
            if (__instance != null && __instance.equipment != null && __instance.equipment.Primary != null)
            {
                // 3. Hat die Primärwaffe unseren Comp?
                var comp = __instance.equipment.Primary.GetComp<CompMultiModeWeapon>();
                if (comp != null)
                {
                    // 4. JA! Dann hol dir die Gizmos direkt aus unserer neuen Public-Methode
                    foreach (var weaponGizmo in comp.GetWeaponGizmos())
                    {
                        yield return weaponGizmo;
                    }
                }
            }
        }
    }
}