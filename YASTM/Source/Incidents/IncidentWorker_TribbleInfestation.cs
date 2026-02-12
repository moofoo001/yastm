using RimWorld;
using Verse;
using System.Collections.Generic;

namespace YASTM
{
    public class IncidentWorker_TribbleInfestation : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;      
            return map.mapTemperature.OutdoorTemp > 0f;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 dropSpot = DropCellFinder.RandomDropSpot(map);

            PawnKindDef tribbleKind = PawnKindDef.Named("ST_PawnKind_Tribble");
            
            if (tribbleKind == null) return false;

            List<Thing> tribbles = new List<Thing>();
            int count = Rand.RangeInclusive(2, 4);

            for (int i = 0; i < count; i++)
            {
                Pawn tribble = PawnGenerator.GeneratePawn(tribbleKind, Faction.OfPlayer);
                tribbles.Add(tribble);
            }

            DropPodUtility.DropThingsNear(dropSpot, map, tribbles);

            SendStandardLetter(parms, new LookTargets(dropSpot, map), "Tribbles!", 
                "A passing trader ship has jettisoned a crate containing small, furry creatures.\n\nThey seem harmless and purr pleasantly. What could go wrong?");

            return true;
        }
    }
}