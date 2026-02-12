using RimWorld;
using Verse;

namespace YASTM
{
    public class IncidentWorker_BadgeyFailure : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)  
        {
            Map map = (Map)parms.target;
            // Only fires if we have power and buildings
            return map.listerBuildings.allBuildingsColonist.Count > 10;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            var comp = map.GetComponent<MapComponent_BadgeyChaos>();
            
            if (comp == null || comp.IsActive) return false;

            comp.TriggerChaos();
            
            SendStandardLetter(parms, null, "Badgey Malfunction", 
                "The holographic tutor 'Badgey' has glitched due to a sub-routine error.\n\nHe is now randomly accessing doors, lights, and security systems to 'teach you a lesson'.\n\nUse a Security Console to purge the AI immediately!");
            
            return true;
        }
    }
}