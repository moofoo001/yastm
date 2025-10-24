using RimWorld;
using Verse;

namespace YASTM
{
    public class IncidentWorker_StartLtJGObjectives : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms) => true;

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var comp = Find.World.GetComponent<WorldComponent_PromotionLtJG>();
            if (comp == null) return false;
            if (comp.Active || comp.RewardGiven) return false; // nur einmal

            comp.Start();
            return true;
        }
    }
}
