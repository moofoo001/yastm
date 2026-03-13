using HarmonyLib;
using RimWorld;
using Verse;

namespace YASTM.Incidents
{
    /// <summary>
    /// block comms console if subspace anomaly is active
    /// </summary>
    [HarmonyPatch(typeof(Building_CommsConsole), "GetFloatMenuOptions")]
    public static class Patch_CommsConsole_SubspaceAnomaly
    {
        [HarmonyPostfix]
        public static void Postfix(Building_CommsConsole __instance, Pawn myPawn, ref System.Collections.Generic.IEnumerable<FloatMenuOption> __result)
        {
            // check if subspace anomaly is active on the current map
            GameConditionDef anomalyDef = DefDatabase<GameConditionDef>.GetNamedSilentFail("ST_SubspaceAnomalyCondition");
            
            if (anomalyDef != null && __instance.Map.gameConditionManager.ConditionIsActive(anomalyDef))
            {
                // if active, clear the list of options (no calling possible) and show disabled error message
                var newList = new System.Collections.Generic.List<FloatMenuOption>();
                
                FloatMenuOption disabledOption = new FloatMenuOption(
                    "Cannot use: Subspace interference", 
                    null, 
                    MenuOptionPriority.Default, 
                    null, null, 0f, null, null, true, 0
                );
                
                newList.Add(disabledOption);
                
                // overwrite vanilla result with disabled list
                __result = newList;
            }
        }
    }
}