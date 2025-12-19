using HarmonyLib;
using Verse;

namespace ST.PhaseWeapons
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            // Debugging aktivieren!
            Harmony.DEBUG = true; 
            
            Log.Message("[YASTM] Initializing Harmony...");
            var h = new Harmony("ST.PhaseWeapons");
            
            try 
            {
                h.PatchAll();
                Log.Message("[YASTM] PatchAll completed successfully.");
            }
            catch (System.Exception e)
            {
                Log.Error($"[YASTM] CRITICAL HARMONY FAILURE: {e}");
            }
        }
    }
}