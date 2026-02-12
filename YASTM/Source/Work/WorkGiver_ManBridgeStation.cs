using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace YASTM
{
    public class WorkGiver_ManBridgeStation : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building building)) return false;

            // have comp ?
            var stationComp = building.GetComp<CompBridgeStation>();
            if (stationComp == null) return false;

            // is bridge mode active ?
            if (!forced)
            {
                var manager = pawn.Map.GetComponent<MapComponent_BridgeManager>();
                if (manager == null || !manager.bridgeManningActive) return false;
            }

            // can reserve ?
            if (!pawn.CanReserve(building, 1, -1, null, forced)) return false;
            if (building.IsForbidden(pawn)) return false;
            
            var power = building.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn) return false;

            // trait check
            if (!string.IsNullOrEmpty(stationComp.Props.requiredTraitDef))
            {
                TraitDef requiredTrait = DefDatabase<TraitDef>.GetNamedSilentFail(stationComp.Props.requiredTraitDef);
                
                // trait exists and pawn does not have it -> reject
                if (requiredTrait != null && (pawn.story == null || !pawn.story.traits.HasTrait(requiredTrait)))
                {
                    //  reason for right-click menu
                    JobFailReason.Is($"Missing required training: {requiredTrait.label}");
                    return false;
                }
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(ST_JobDefOf.ST_Job_ManBridgeStation, t);
        }
    }
}