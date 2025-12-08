using HarmonyLib;
using RimWorld;
using Verse;

namespace YASTM.MapSystems
{
   
    [StaticConstructorOnStartup]
    public static class EnsureMapComponentsBootstrap
    {
        static EnsureMapComponentsBootstrap()
        {
           
            var id = "YASTM.EnsureMapComponents";
            if (Harmony.HasAnyPatches(id) == false)
            {
                new Harmony(id).PatchAll();
            }
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class Patch_Map_FinalizeInit_EnsureObeliskFlow
    {
        static void Postfix(Map __instance)
        {
            // Schon vorhanden?
            var comp = __instance.GetComponent<MapComponent_ObeliskFlow>();
            if (comp == null)
            {
                comp = new MapComponent_ObeliskFlow(__instance);
                __instance.components.Add(comp);
            }
        }
    }
}
