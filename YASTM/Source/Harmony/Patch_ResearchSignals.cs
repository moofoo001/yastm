// Source/Debug/YastmHarmonyInit_Research.cs
using HarmonyLib;
using Verse;

namespace StarTrekFactions.Debug
{
    [StaticConstructorOnStartup]
    public static class YastmHarmonyInit_Research
    {
        static YastmHarmonyInit_Research()
        {
            var h = new Harmony("YASTM.Debug.Research");

        }
    }
}
