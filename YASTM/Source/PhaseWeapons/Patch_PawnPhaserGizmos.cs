using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using ST.PhaseWeapons;

namespace YASTM.Phasers
{
    /// <summary>
    /// Adds phaser fire-mode gizmos to drafted pawns that are holding a phaser.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_PhaserGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var g in __result)
                yield return g;

            if (!__instance.Drafted)
                yield break;

            if (__instance.Faction != Faction.OfPlayer)
                yield break;

            var eq = __instance.equipment;
            if (eq == null)
                yield break;

            var primary = eq.Primary;
            if (primary == null)
                yield break;

            var comp = PhaserUtil.GetPhaserComp(primary);
            if (comp == null)
                yield break;

            foreach (var gizmo in comp.CompGetGizmosExtra())
                yield return gizmo;
        }
    }
}
