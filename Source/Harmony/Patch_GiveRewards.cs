// Source/Harmony/Patch_GiveRewards.cs
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace StarTrekFactions.HarmonyPatches
{
    [HarmonyPatch]
    public static class Patch_GiveRewards_RunInt
    {
        // internen Typ & Methode per String auflösen
        static MethodBase TargetMethod() =>
            AccessTools.Method("RimWorld.QuestGen.QuestNode_GiveRewards:RunInt");

        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                Log.Warning("[YASTM] Suppressed GiveRewards exception: " + __exception.GetType().Name);
                return null; // Exception unterdrücken
            }
            return null;
        }
    }
}
