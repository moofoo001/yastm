// YASTM.IdeoFix — stellt sicher, dass neu erzeugte Humanlikes immer eine Ideo haben
using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace YASTM.IdeoFix
{
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    public static class Patch_EnsureIdeoOnGenerate
    {
        static void Postfix(Pawn __result)
        {
            if (__result == null || !ModsConfig.IdeologyActive) return;
            if (!__result.RaceProps?.Humanlike ?? true) return;

            var p = __result;

            if (p.ideo == null || p.ideo.Ideo == null)
            {
                Ideo target = null;

                // 1) Ideo der eigenen Fraktion bevorzugen
                target = p.Faction?.ideos?.PrimaryIdeo; // <- geändert

                // 2) Falls direkt als Spieler-Pawn erzeugt
                if (target == null && p.Faction == Faction.OfPlayer)
                    target = Faction.OfPlayer?.ideos?.PrimaryIdeo; // <- geändert

                // 3) Fallback: irgendeine Welt-Ideo
                if (target == null)
                    target = Find.IdeoManager.IdeosListForReading.FirstOrDefault();

                if (target != null && p.ideo != null)
                {
                    // Signatur ohne bool-Parameter (RW 1.6)
                    p.ideo.SetIdeo(target); // <- geändert
                }
            }
        }
    }
}
