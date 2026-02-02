using Verse;
using RimWorld;
using System.Collections.Generic;

namespace YASTM
{
    public class PlaceWorker_OnShuttleBay : PlaceWorker
    {

        private const string LandingPadDefName = "ST_Shuttle_LandingPad";

        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            List<Thing> thingsHere = map.thingGrid.ThingsListAt(loc);

            for (int i = 0; i < thingsHere.Count; i++)
            {
                Thing t = thingsHere[i];
                
                // is landing pad 
                if (t.def != null && t.def.defName == LandingPadDefName)
                {
                    return true;
                }
                

                //plan both
                if (t.def != null && t.def.entityDefToBuild != null && t.def.entityDefToBuild.defName == LandingPadDefName)
                {
                    return true;
                }
            }

            // fallback 
            return "ST_MustPlaceOnShuttleBay".Translate(); 
        }
    }
}