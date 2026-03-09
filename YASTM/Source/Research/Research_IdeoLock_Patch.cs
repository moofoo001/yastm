using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM.Research
{
    public class ResearchRequiresMemeExtension : DefModExtension
    {
        public List<string> requiredMemes;
        public List<string> disallowedMemes;
        
        // Optional: Ein eigener Text in der XML, z.B. <lockReason>Requires Klingon Honor meme.</lockReason>
        public string lockReason; 
    }

    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.CanStartNow), MethodType.Getter)]
    public static class Research_IdeoLock_Patch
    {
        [HarmonyPostfix]
        public static void CanStartNow_Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result)
                return;

            if (!YASTM_Mod.Settings.enableFactionResearchLock)
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

            // Check required memes
            if (ext.requiredMemes != null)
            {
                foreach (string defName in ext.requiredMemes)
                {
                    if (string.IsNullOrEmpty(defName)) continue;
                    MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(defName);
                    if (meme == null || !ideo.memes.Contains(meme))
                    {
                        __result = false;
                        return;
                    }
                }
            }

            // Check disallowed memes
            if (ext.disallowedMemes != null)
            {
                foreach (string defName in ext.disallowedMemes)
                {
                    if (string.IsNullOrEmpty(defName)) continue;
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

    /// <summary>
    /// This patch appends our warning directly to the description 
    /// in the research menu if the faction lock is active.
    /// </summary>
    [HarmonyPatch(typeof(ResearchProjectDef), "Description", MethodType.Getter)]
    public static class Patch_ResearchProjectDef_Description
    {
        public static void Postfix(ResearchProjectDef __instance, ref string __result)
        {
            if (!YASTM_Mod.Settings.enableFactionResearchLock)
                return;

            var ext = __instance.GetModExtension<ResearchRequiresMemeExtension>();
            if (ext == null)
                return;

            // check if research is locked
            bool isLocked = false;
            string lockMessage = "\n\n<color=#ff4d4d><b>LOCKED:</b> Incompatible Faction Ideology</color>";

            Ideo ideo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (ideo == null) 
            {
                isLocked = true;
            }
            else
            {
                if (ext.requiredMemes != null)
                {
                    foreach (string defName in ext.requiredMemes)
                    {
                        MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(defName);
                        if (meme == null || !ideo.memes.Contains(meme))
                        {
                            isLocked = true;
                            // If you have defined a lockReason in the XML, it will use it!
                            if (!string.IsNullOrEmpty(ext.lockReason)) 
                                lockMessage = "\n\n<color=#ff4d4d><b>LOCKED:</b> " + ext.lockReason + "</color>";
                            break;
                        }
                    }
                }
                
                if (!isLocked && ext.disallowedMemes != null)
                {
                    foreach (string defName in ext.disallowedMemes)
                    {
                        MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(defName);
                        if (meme != null && ideo.memes.Contains(meme))
                        {
                            isLocked = true;
                            if (!string.IsNullOrEmpty(ext.lockReason)) 
                                lockMessage = "\n\n<color=#ff4d4d><b>LOCKED:</b> " + ext.lockReason + "</color>";
                            break;
                        }
                    }
                }
            }

            // If locked, append the red text to the vanilla description!
            if (isLocked && !__result.Contains("LOCKED:"))
            {
                __result += lockMessage;
            }
        }
    }
}