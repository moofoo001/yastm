using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Work
{
    /// <summary>
    /// Legacy workgiver that used to create direct obelisk-scan jobs.
    /// Scanning is now operated from the Science Console, so this yields no work.
    /// </summary>
    public class WorkGiver_ScanObelisk : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            yield break; // disabled
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => false;

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) => null;
    }
}

