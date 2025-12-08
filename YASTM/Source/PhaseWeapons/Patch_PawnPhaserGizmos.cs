using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ST.PhaseWeapons
{
    /// <summary>
    ///  Phaser-Mode-Gizmos on Pawn-Command-Bar 
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_PawnPhaserGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {

            if (__result != null)
            {
                foreach (var g in __result)
                    yield return g;
            }


            if (__instance == null)
                yield break;
            if (!__instance.Drafted)
                yield break;
            if (__instance.Faction != Faction.OfPlayer)
                yield break;

 
            var primary = __instance.equipment?.Primary;
            if (primary == null)
                yield break;


            var phaserComp = PhaserUtil.GetPhaserComp(primary);
            if (phaserComp == null)
                yield break;


            var extra = phaserComp.CompGetGizmosExtra();
            if (extra == null)
                yield break;

            foreach (var gizmo in extra)
                yield return gizmo;
        }
    }
}
