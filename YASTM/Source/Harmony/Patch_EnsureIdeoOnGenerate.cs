// Mods/YASTM/Source/Harmony/Patch_EnsureIdeoOnGenerate.cs
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

                
                target = p.Faction?.ideos?.PrimaryIdeo;

                
                if (target == null && p.Faction == Faction.OfPlayer)
                    target = Faction.OfPlayer?.ideos?.PrimaryIdeo; 

                
                if (target == null)
                    target = Find.IdeoManager.IdeosListForReading.FirstOrDefault();

                if (target != null && p.ideo != null)
                {
                    
                    p.ideo.SetIdeo(target); 
                }
            }
        }
    }
}

