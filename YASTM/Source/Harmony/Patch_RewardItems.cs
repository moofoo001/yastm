// Source/Harmony/Patch_RewardItems.cs
using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StarTrekFactions.HarmonyPatches
{
    [HarmonyPatch(typeof(Reward_Items), "GenerateQuestParts")]
    public static class Patch_Reward_Items_GenerateQuestParts
    {
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                Log.Warning("[YASTM] Suppressed Reward_Items exception: " + __exception.GetType().Name);
                return null;
            }
            return null;
        }
    }
}

