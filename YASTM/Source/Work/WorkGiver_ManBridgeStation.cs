using RimWorld;
using Verse;
using Verse.AI;
using YASTM.Source.Comps;

namespace YASTM.Source.WorkGivers
{
    public class WorkGiver_ManBridgeStation : WorkGiver_Scanner
    {
        // set the thing request to artificial buildings
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // checks: if thing is valid
            if (!t.Spawned || t.IsForbidden(pawn)) return false;

            // checks: specific to Bridge Station
            var bridgeComp = t.TryGetComp<CompBridgeStation>();
            if (bridgeComp == null) 
            {
                return false;
            }
            // -----------------------------

            // power check
            CompPowerTrader power = t.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                if (forced) JobFailReason.Is("No Power");
                return false;
            }

            // checks: reservation
            if (!pawn.CanReserve(t, 1, -1, null, forced)) 
            {
                return false;
            }

            // reachability
            if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly))
            {
                if (forced) JobFailReason.Is("Cannot reach");
                return false;
            }

            // already
            if (pawn.CurJob != null && pawn.CurJob.def.defName == "ST_Job_ManBridgeStation" && pawn.CurJob.targetA.Thing == t)
            {
                return false;
            }

            // optional needs check
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