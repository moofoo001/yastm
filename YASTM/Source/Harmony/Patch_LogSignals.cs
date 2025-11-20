using System;
using System.Reflection;
using HarmonyLib;

namespace StarTrekFactions.Debug
{
    [Verse.StaticConstructorOnStartup]
    public static class YastmHarmonyInit_Signals
    {
        static YastmHarmonyInit_Signals()
        {
            try
            {
                var h = new Harmony("yastm.debug.signals");
                var smType = AccessTools.TypeByName("Verse.SignalManager");
                var sigType= AccessTools.TypeByName("Verse.Signal");
                if (smType == null || sigType == null) { Verse.Log.Warning("[YASTM] Signal types missing"); return; }
                var send = AccessTools.Method(smType, "SendSignal", new[] { sigType });
                if (send == null) { Verse.Log.Warning("[YASTM] SendSignal() missing"); return; }
                h.Patch(send, prefix: new HarmonyMethod(typeof(Patch_LogSignals).GetMethod(nameof(Patch_LogSignals.Prefix))));
                Verse.Log.Message("[YASTM] STQ signal logger active");
            }
            catch (Exception e) { Verse.Log.Warning("[YASTM] Signal logger init failed: " + e); }
        }
    }

    public static class Patch_LogSignals
    {
        public static void Prefix(object __0)
        {
            if (__0 == null) return;
            try
            {
                var tag = AccessTools.Field(__0.GetType(), "tag")?.GetValue(__0) as string ?? "";
                if (!tag.StartsWith("STQ.")) return;
                Verse.Log.Message($"[YASTM][SIGNAL] {tag}");
            }
            catch { /* ignore */ }
        }
    }
}

