// YASTM.SafeUI — Social-Tab Guard (RW 1.6, inkl. Culture-Check)
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Reflection;

namespace YASTM.SafeUI
{
    [HarmonyPatch(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawPawnRoleSelection))]
    public static class Patch_SocialCardRoleGuard
    {
        static bool Prefix(Pawn pawn, Rect rect)
        {
            if (!ModsConfig.IdeologyActive) return true;
            if (pawn == null) return false;

            var playerIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            var pawnIdeo   = pawn.ideo?.Ideo;

            if (playerIdeo == null || pawnIdeo == null) return false;
            if (!HasCulture(playerIdeo) || !HasCulture(pawnIdeo)) return false;

            return true; 
        }

        
        private static bool HasCulture(Ideo ideo)
        {
            if (ideo == null) return false;
            var t = typeof(Ideo);
            var prop = t.GetProperty("culture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var fld  = t.GetField("cultureInt", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? t.GetField("culture",    BindingFlags.Instance | BindingFlags.NonPublic);

            var val = prop?.GetValue(ideo) ?? fld?.GetValue(ideo);
            return val != null;
        }
    }
}
