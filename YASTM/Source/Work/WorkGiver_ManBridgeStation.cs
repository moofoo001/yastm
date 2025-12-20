using RimWorld;
using Verse;
using Verse.AI;
using YASTM.Source.Comps; // WICHTIG: Namespace für den Comp

namespace YASTM.Source.WorkGivers
{
    public class WorkGiver_ManBridgeStation : WorkGiver_Scanner
    {
        // Wir scannen alles Künstliche (das ist okay, solange wir gleich filtern)
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // 1. Basis-Checks
            if (!t.Spawned || t.IsForbidden(pawn)) return false;

            // --- DER TÜR-STOPPER (FIX) ---
            // Wir prüfen, ob das Gebäude überhaupt unsere Komponente hat.
            // Wenn nicht (z.B. eine Tür oder Lampe), brechen wir sofort ab.
            var bridgeComp = t.TryGetComp<CompBridgeStation>();
            if (bridgeComp == null) 
            {
                return false;
            }
            // -----------------------------

            // 2. Strom
            CompPowerTrader power = t.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                if (forced) JobFailReason.Is("No Power");
                return false;
            }

            // 3. Reservierung
            if (!pawn.CanReserve(t, 1, -1, null, forced)) 
            {
                return false;
            }

            // 4. Pfad-Check (OnCell)
            if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly))
            {
                if (forced) JobFailReason.Is("Cannot reach");
                return false;
            }

            // 5. Anti-Loop
            if (pawn.CurJob != null && pawn.CurJob.def.defName == "ST_Job_ManBridgeStation" && pawn.CurJob.targetA.Thing == t)
            {
                return false;
            }

            // 6. Bedürfnisse (Pause machen)
            if (!forced) 
            {
                if (pawn.needs.food != null && pawn.needs.food.CurLevelPercentage < 0.30f) return false;
                if (pawn.needs.rest != null && pawn.needs.rest.CurLevelPercentage < 0.30f) return false;
                if (pawn.needs.joy != null && pawn.needs.joy.CurLevelPercentage < 0.10f) return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_Job_ManBridgeStation"), t);
        }
    }
}