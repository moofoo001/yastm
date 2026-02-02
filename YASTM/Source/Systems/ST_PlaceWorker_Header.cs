using Verse;
using RimWorld;

namespace YASTM
{
    public class PlaceWorker_Header : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            //return new AcceptanceReport("ST_SelectSubItem".Translate());
            return "ST_SelectSubItem".Translate(); 
            // Text: "Please select a specific item from the list."
        }
    }
}