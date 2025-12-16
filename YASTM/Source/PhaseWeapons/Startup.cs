// Source/PhaseWeapons/Startup.cs
using HarmonyLib;
using Verse;

namespace ST.PhaseWeapons
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            var h = new Harmony("ST.PhaseWeapons");
            h.PatchAll();
        }
    }
}
