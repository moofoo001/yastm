using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Work
{
 
    public class WorkGiver_ScanObelisk : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            yield break; 
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => false;

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) => null;
    }
}

