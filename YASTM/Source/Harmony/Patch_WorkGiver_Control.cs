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
        // Wir nutzen einen Timer, um das Log nicht zu fluten (nur alle 100 Aufrufe)
        private static int debugCounter = 0;

        public static void Postfix(Pawn pawn, Thing thing, bool forced, ref Job __result)
        {
            // Wenn schon kein Job da ist, brauchen wir nichts tun
            if (__result == null) return;

            // --- REPLIKATOR CHECK ---
            if (thing.def.defName == "ST_Replicator")
            {
                 bool hasFuel = false;
                 string failReason = "No Link Comp";

                 var linkComp = thing.TryGetComp<CompAffectedByFacilities>();
                 
                 if (linkComp != null)
                 {
                     // Debugging alle paar Ticks, damit das Log lesbar bleibt
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
                     // Job blockieren
                     __result = null;
                     
                     // Dem Spieler sagen, warum (wenn er den Pawn zwingt/Rechtsklick macht)
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