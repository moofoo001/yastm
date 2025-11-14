// Source/UI/InspectStringCleaner_Init.cs
using HarmonyLib;
using Verse;

namespace YASTM.UI
{
    [StaticConstructorOnStartup]
    public static class InspectStringCleaner_Init
    {
        static InspectStringCleaner_Init()
        {
            var h = new Harmony("YASTM.UI.InspectCleaner");
        }
    }
}