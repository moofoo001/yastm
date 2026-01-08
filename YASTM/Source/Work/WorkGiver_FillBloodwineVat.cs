using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class WorkGiver_FillBloodwineVat : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDef.Named("ST_Building_BloodwineVat"));
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_WorkTable) && !(t is Building)) return false;
            
            var comp = t.TryGetComp<CompBloodwineVat>();
            if (comp == null || comp.Fermented || comp.Full) return false;

            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced)) return false;

            // Suche nach Würmern im Inventar oder auf der Map
            if (FindWorms(pawn) == null) 
            {
                JobFailReason.Is("ST_NoWorms".Translate());
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompBloodwineVat>();
            Thing worms = FindWorms(pawn);
            
            if (worms != null)
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_Job_FillBloodwineVat"), t, worms);
                job.count = CompBloodwineVat.MaxCapacity - comp.wormCount;
                return job;
            }
            return null;
        }

        private Thing FindWorms(Pawn pawn)
        {
            // Prio 1: Inventar
            // Prio 2: Map
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, 
                ThingRequest.ForDef(ThingDef.Named("ST_RawSerpentWorms")), 
                PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, 
                x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
        }
    }
}