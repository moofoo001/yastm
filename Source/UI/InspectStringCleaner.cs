using HarmonyLib;
using System.Text.RegularExpressions;
using Verse;

namespace YASTM.UI
{
    [StaticConstructorOnStartup]
    public static class InspectStringCleaner_Init
    {
        static InspectStringCleaner_Init()
        {
            new Harmony("YASTM.UI.InspectClean").PatchAll();
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetInspectString))]
    public static class Patch_ThingWithComps_GetInspectString
    {
        static void Postfix(ThingWithComps __instance, ref string __result)
        {
            if (__result.NullOrEmpty()) return;

            var defName = __instance.def?.defName;
            // nur unsere Transporter-Teile anfassen
            if (defName == "ST_TransporterPad" || defName == "ST_TransporterConsole")
            {
                // Mehrfach-Leerzeilen zu einer zusammenfassen + trailing Newlines kappen
                __result = Regex.Replace(__result, @"(\r?\n)\s*(\r?\n)+", "$1");
                __result = __result.TrimEnd();
            }
        }
    }
}
