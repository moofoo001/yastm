using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace YASTM.Research
{
    /// <summary>
    /// ModExtension für ResearchProjectDefs:
    /// bindet Forschung an bestimmte Memes (required / disallowed).
    /// 
    /// XML-Beispiel:
    /// <modExtensions>
    ///   <li Class="YASTM.Research.ResearchRequiresMemeExtension">
    ///     <requiredMemes>
    ///       <li>ST_Meme_KlingonHonor</li>
    ///     </requiredMemes>
    ///   </li>
    /// </modExtensions>
    /// </summary>
    public class ResearchRequiresMemeExtension : DefModExtension
    {
        public List<string> requiredMemes;
        public List<string> disallowedMemes;
    }

    /// <summary>
    /// Patch auf ResearchProjectDef.CanStartNow (Getter):
    /// Falls ein Projekt die Extension hat, wird geprüft, ob die
    /// Player-Ideo passende Memes hat.
    /// </summary>
    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.CanStartNow), MethodType.Getter)]
    public static class Research_IdeoLock_Patch
    {
        [HarmonyPostfix]
        public static void CanStartNow_Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            // Vanilla blockt schon? -> wir lassen es so.
            if (!__result)
                return;

            var ext = __instance.GetModExtension<ResearchRequiresMemeExtension>();
            if (ext == null)
                return;

            Ideo ideo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (ideo == null)
            {
                __result = false;
                return;
            }

            // requiredMemes: alle müssen vorhanden sein
            if (ext.requiredMemes != null)
            {
                foreach (string defName in ext.requiredMemes)
                {
                    if (string.IsNullOrEmpty(defName))
                        continue;

                    MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(defName);
                    if (meme == null || !ideo.memes.Contains(meme))
                    {
                        __result = false;
                        return;
                    }
                }
            }

            // disallowedMemes: irgendeins vorhanden -> blockieren
            if (ext.disallowedMemes != null)
            {
                foreach (string defName in ext.disallowedMemes)
                {
                    if (string.IsNullOrEmpty(defName))
                        continue;

                    MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(defName);
                    if (meme != null && ideo.memes.Contains(meme))
                    {
                        __result = false;
                        return;
                    }
                }
            }
        }
    }
}
