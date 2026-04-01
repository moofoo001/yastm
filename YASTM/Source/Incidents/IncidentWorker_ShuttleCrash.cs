using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class IncidentWorker_ShuttleCrash : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // search save spot 4 crash
            if (!DropCellFinder.TryFindDropSpotNear(map.Center, map, out IntVec3 dropSpot, false, false, false))
            {
                return false;
            }

            // create shuttle 
            ThingDef shuttleDef = ThingDef.Named("ST_Shuttle_Taxi");
            Thing shuttle = ThingMaker.MakeThing(shuttleDef);
            
            // Set the faction to "Player" so the player can repair it
            shuttle.SetFaction(Faction.OfPlayer); 
            
            // Set the hitpoints to critical 15%
            shuttle.HitPoints = Mathf.Max(1, Mathf.RoundToInt(shuttle.MaxHitPoints * 0.15f));

            // Remove any Dilithium/Fuel from the tank (burned up in the crash)
            CompRefuelable fuelComp = shuttle.TryGetComp<CompRefuelable>();
            if (fuelComp != null)
            {
                fuelComp.ConsumeFuel(fuelComp.Fuel);
            }

            // spawn shuttle as skyfaller   
            ThingDef skyfallerDef = ThingDef.Named("ST_Shuttle_Crashing");
            SkyfallerMaker.SpawnSkyfaller(skyfallerDef, shuttle, dropSpot, map);

            // generate survivor
            Faction faction = Find.FactionManager.RandomNonHostileFaction(false, false, false, TechLevel.Spacer);
            PawnGenerationRequest request = new PawnGenerationRequest(
                PawnKindDefOf.SpaceRefugee, 
                faction, 
                PawnGenerationContext.NonPlayer, 
                -1, 
                forceGenerateNewPawn: true, 
                allowDead: false, 
                allowDowned: true, 
                canGeneratePawnRelations: true, 
                mustBeCapableOfViolence: false);

            Pawn survivor = PawnGenerator.GeneratePawn(request);
            
            
            HealthUtility.DamageUntilDowned(survivor, true);

            // set survivor next to shuttle
            IntVec3 podSpot = dropSpot + new IntVec3(0, 0, -3); // Slightly offset
            if (!podSpot.InBounds(map)) podSpot = dropSpot;

            
            List<Thing> thingsToDrop = new List<Thing> { survivor };
            DropPodUtility.DropThingsNear(podSpot, map, thingsToDrop, 110, canInstaDropDuringInit: false, leaveSlag: true);

            // notify
            SendStandardLetter(parms, new TargetInfo(dropSpot, map));

            return true;
        }
    }
}