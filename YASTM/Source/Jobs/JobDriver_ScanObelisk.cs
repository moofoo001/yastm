using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Jobs
{

    public class JobDriver_ScanObelisk : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var cancel = new Toil
            {
                initAction = () =>
                {
                    Messages.Message(
                        "Obelisk scanning has moved to the Science Console. Use the console to begin a scan.",
                        pawn, MessageTypeDefOf.RejectInput, historical: false);
                    EndJobWith(JobCondition.Incompletable);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return cancel;
        }
    }
}

