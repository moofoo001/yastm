using HarmonyLib;
using RimWorld;
using Verse;

namespace StarTrekFactions.Debug
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class Patch_ResearchSignals
    {
        static void Postfix(ResearchProjectDef proj)
        {
            if (proj == null) return;

            // Heuristik: erkenne eure beiden Scan-Projekte am defName/Label
            var dn = proj.defName ?? "";
            var lb = proj.label ?? "";

            bool isScanA = dn.Contains("Obelisk", true) && (dn.Contains("ScanA", true) || dn.EndsWith("_A", true))
                        || lb.Contains("Obelisk", true) && lb.Contains("A", true);
            bool isScanB = dn.Contains("Obelisk", true) && (dn.Contains("ScanB", true) || dn.EndsWith("_B", true))
                        || lb.Contains("Obelisk", true) && lb.Contains("B", true);

            if (isScanA)
            {
                Log.Message($"[YASTM][Research] '{proj.defName}' complete -> STQ.Obelisks.ScanA.Completed");
                Find.SignalManager.SendSignal(new Signal("STQ.Obelisks.ScanA.Completed"));
            }
            if (isScanB)
            {
                Log.Message($"[YASTM][Research] '{proj.defName}' complete -> STQ.Obelisks.ScanB.Completed");
                Find.SignalManager.SendSignal(new Signal("STQ.Obelisks.ScanB.Completed"));
            }
        }

        static bool Contains(this string s, string sub, bool ignoreCase) =>
            s?.IndexOf(sub, ignoreCase ? System.StringComparison.OrdinalIgnoreCase
                                        : System.StringComparison.Ordinal) >= 0;
        static bool EndsWith(this string s, string suffix, bool ignoreCase) =>
            s?.EndsWith(suffix, ignoreCase ? System.StringComparison.OrdinalIgnoreCase
                                           : System.StringComparison.Ordinal) == true;
    }

    [StaticConstructorOnStartup]
    public static class YastmHarmonyInit_Research
    {
        static YastmHarmonyInit_Research() { new Harmony("yastm.debug.research").PatchAll(); }
    }
}
