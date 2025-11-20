using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YASTM.Jobs
{
    /// <summary>
    /// Legacy job that previously made pawns walk to obelisks and scan.
    /// Scanning is now operated from the Science Console; this job aborts immediately
    /// with a player-facing message, to prevent old queued jobs from running.
    /// </summary>
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

