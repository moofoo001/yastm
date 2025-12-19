using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using YASTM.Source.Comps;
using System.Linq;

namespace YASTM.Source.HarmonyPatches
{
    // Dieser Patch kontrolliert, ob ein Job überhaupt angeboten wird
    [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing")]
    public static class Patch_WorkGiver_Control
    {
        public static void Postfix(Pawn pawn, Thing thing, bool forced, ref Job __result)
        {
            // Wenn das Spiel schon sagt "Kein Job", sind wir fertig.
            if (__result == null) return;

            // 1. Matter Converter: Pause wenn voll
            if (thing.def.defName == "ST_MatterConverter")
            {
                var tank = thing.TryGetComp<CompMatterTank>();
                // Toleranz von 1.0f, damit er nicht bei 99.9 aufhört
                if (tank != null && tank.storedMatter >= (tank.Props.capacity - 1.0f))
                {
                    // Job blockieren
                    // JobFailReason.Is("Matter tank is full."); // Optional: Zeigt Grund bei Rechtsklick
                    __result = null; 
                }
            }

            // 2. Replicator: Pause wenn leer
            if (thing.def.defName == "ST_Replicator")
            {
                // Kosten schätzen (Mahlzeit = 1, Item = 5)
                float cost = 5f;
                if (__result.bill is Bill_Production billProd && billProd.recipe.defName.Contains("Meal"))
                {
                    cost = 1f;
                }

                // Verbundenen Tank suchen
                bool hasEnough = false;
                var facilityComp = thing.TryGetComp<CompAffectedByFacilities>();
                if (facilityComp != null)
                {
                    foreach (var fac in facilityComp.LinkedFacilitiesListForReading)
                    {
                        var tank = fac.TryGetComp<CompMatterTank>();
                        if (tank != null && tank.storedMatter >= cost)
                        {
                            hasEnough = true;
                            break;
                        }
                    }
                }

                if (!hasEnough)
                {
                    // Job blockieren
                    JobFailReason.Is("ST_ReplicatorNoMatter".Translate());
                    __result = null;
                }
            }
        }
    }
}