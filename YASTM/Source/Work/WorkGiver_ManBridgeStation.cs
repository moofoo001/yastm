using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Source.WorkGivers
{
    public class WorkGiver_ManBridgeStation : WorkGiver_Scanner
    {

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        private static readonly JobDef JobDefName = DefDatabase<JobDef>.GetNamed("ST_Job_ManBridgeStation");

 public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // 1. Standard-Checks
            if (!t.Spawned || t.IsForbidden(pawn)) return false;
            
            // 2. Strom & Status Check
            CompPowerTrader power = t.TryGetComp<CompPowerTrader>();
            CompBreakdownable breakdown = t.TryGetComp<CompBreakdownable>();
            
            if (power != null && !power.PowerOn) return false;
            if (breakdown != null && breakdown.BrokenDown) return false;

            // 3. Reservierung
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;


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
            return JobMaker.MakeJob(JobDefName, t);
        }
    }
}