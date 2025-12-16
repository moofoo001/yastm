using Verse;
using Verse.AI;
using RimWorld;
using YASTM.Source.Comps; 

namespace YASTM.Source.Work
{
    public class WorkGiver_ManBridgeStation : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                // Wir suchen nach allen Gebäuden
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        // Prüft, ob das Gebäude ein Job-Kandidat ist
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building building = t as Building;
            if (building == null) return false;

            // Hat es unsere Brücken-Komponente?
            var bridgeComp = building.TryGetComp<CompBridgeStation>();
            if (bridgeComp == null) return false;

            // Ist es "mannable"? (Der Captain's Chair ist mannable, ein Deko-Tisch nicht)
            if (!bridgeComp.Props.mannable) return false;

            // Standard RimWorld Checks (Reservierbar? Erreichbar? Strom?)
            if (!pawn.CanReserve(building, 1, -1, null, forced)) return false;
            if (building.IsForbidden(pawn)) return false;
            
            // Strom-Check
            var powerComp = building.GetComp<CompPowerTrader>();
            if (powerComp != null && !powerComp.PowerOn) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // Erstellt den Job, der auf XML verweist
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_Job_ManBridgeStation"), t);
        }
    }
}