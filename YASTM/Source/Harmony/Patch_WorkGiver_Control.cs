using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using YASTM; 
using System.Linq;

namespace YASTM.Source.HarmonyPatches
{
    [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing")]
    public static class Patch_WorkGiver_Control
    {
        // Debug counter to limit log spam
        private static int debugCounter = 0;

        public static void Postfix(Pawn pawn, Thing thing, bool forced, ref Job __result)
        {
            // If no job is assigned, nothing to do
            if (__result == null) return;

            // --- REPLIKATOR CHECK ---
            if (thing.def.defName == "ST_Replicator")
            {
                 bool hasFuel = false;
                 string failReason = "No Link Comp";

                 var linkComp = thing.TryGetComp<CompAffectedByFacilities>();
                 
                 if (linkComp != null)
                 {
                     // Debugging: Log every 100th check when forced
                     bool doDebug = (debugCounter++ % 100 == 0) && forced; 

                     if (doDebug) Log.Message($"[YASTM DEBUG] Replicator Check for {pawn.Name.ToStringShort}: Found {linkComp.LinkedFacilitiesListForReading.Count} facilities.");

                     foreach(var fac in linkComp.LinkedFacilitiesListForReading)
                     {
                         var t = fac.TryGetComp<CompMatterTank>();
                         if (t != null)
                         {
                             if (doDebug) Log.Message($"[YASTM DEBUG] - Found Tank. Matter: {t.storedMatter}/{t.MaxCapacity}");
                             
                             if (t.storedMatter >= 1.0f) 
                             {
                                 hasFuel = true; 
                                 break; 
                             }
                             else
                             {
                                 failReason = "Tank Empty";
                             }
                         }
                         else
                         {
                             if (doDebug) Log.Message($"[YASTM DEBUG] - Facility {fac.Label} has no CompMatterTank.");
                         }
                     }
                 }

                 if (!hasFuel) 
                 {
                     // Block the job
                     __result = null;
                     
                     // Log reason if forced
                     if (forced)
                     {
                         JobFailReason.Is("Replicator Error: " + failReason);
                         Log.Warning($"[YASTM] Replicator job blocked. Reason: {failReason}");
                     }
                 }
            }
        }
    }
}