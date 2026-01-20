using HarmonyLib;
using Verse;
using System.Reflection;

namespace YASTM
{
    // Diese Klasse startet Harmony automatisch, sobald das Spiel lädt
    [StaticConstructorOnStartup]
    public static class YASTM_Loader
    {
        static YASTM_Loader()
        {
            // Hier geben wir deiner Mod eine einzigartige ID
            var harmony = new Harmony("com.ectos.star.trek.mod");
            
            // Befehl: "Suche alle Patches in diesem Code und schalte sie ein!"
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            Log.Message("==========================================");
            Log.Message("[YASTM] Harmony Patches wurden geladen! 🖖");
            Log.Message("==========================================");
        }
    }
}