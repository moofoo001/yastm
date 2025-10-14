using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace StarTrekFactions.Work
{
    public class WorkGiver_ScanObelisk : WorkGiver_Scanner
    {
        // Damit Rechtsklick möglich ist (Pawn darf „berühren“)
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        // Wenn du willst, an einen WorkType binden (z.B. Research)
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            // Biete alle Things mit CompScanWork an, die noch NICHT fertig sind
            var map = pawn.Map;
            if (map == null) yield break;

            foreach (var t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (t.DestroyedOrNull()) continue;
                var comp = t.TryGetComp<StarTrekFactions.Comps.CompScanWork>();
                if (comp == null) continue;
                if (comp.Completed) continue;
                yield return t;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t == null || t.Destroyed) return false;

            // Drafted, Zeitplan, etc.
            if (pawn.Drafted)
            {
                JobFailReason.Is("Drafted".Translate());
                return false;
            }

            // Fähigkeiten/WorkType (falls gewünscht)
            // if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Research)) return false;

            // Basic checks
            if (t.IsForbidden(pawn))
            {
                JobFailReason.Is("Forbidden".Translate());
                return false;
            }
            if (!pawn.CanReserve(t))
            {
                JobFailReason.Is("Reserved".Translate());
                return false;
            }
            if (!pawn.CanReach(t, PathEndMode.Touch, Danger.Some))
            {
                JobFailReason.Is("NoPath".Translate());
                return false;
            }

            var comp = t.TryGetComp<StarTrekFactions.Comps.CompScanWork>();
            if (comp == null) return false;

            // Schon fertig?
            if (comp.Completed)
            {
                JobFailReason.Is("Scan already completed");
                return false;
            }

            // Sensorcheck – zeigt dir beim Rechtsklick den Grund an
            if (!comp.PoweredSensorNearby(pawn))
            {
                JobFailReason.Is("Requires powered Subspace Sensor nearby");
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!HasJobOnThing(pawn, t, forced)) return null;
            var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_ScanObelisk"), t);
            job.playerForced = forced; // Rechtsklick = forced
            return job;
        }
    }
}
