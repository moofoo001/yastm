using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace YASTM.Research
{
    /// <summary>
    /// ModExtension for ResearchProjectDefs:
    /// bind to faction (required / disallowed).
    /// 
    /// XML-example
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
    /// Patch to enforce meme requirements for research projects.
    /// </summary>
    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.CanStartNow), MethodType.Getter)]
    public static class Research_IdeoLock_Patch
    {
        [HarmonyPostfix]
        public static void CanStartNow_Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            // already false -> no need to check further
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

            // requiredMemes: all must be present
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

            // disallowedMemes: none must be present
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
