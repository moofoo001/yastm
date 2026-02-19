using RimWorld;
using Verse;
using System.Linq;

namespace YASTM
{
    public class IncidentWorker_BadgeyBetrayal : IncidentWorker
    {
        // can fire now sub
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Check if the mod is enabled
            if (!YASTM_Mod.Settings.enableBadgeyIncidents) return false;
            
            if (!base.CanFireNowSub(parms)) return false;
            Map map = (Map)parms.target;

            if (map == null) return false;

            // check if holoemitter exsists

            bool hasEmitter = map.listerBuildings.allBuildingsColonist
                .Any(b => b.def.defName == "ST_HoloEmitter");

            if (!hasEmitter) return false;

            // check if security console exsists
            bool hasSecurityConsole = map.listerBuildings.allBuildingsColonist
                .Any(b => b.GetComp<CompBadgeyShutdown>() != null);

            if (!hasSecurityConsole) return false;

            // check if badgey is already active
            var comp = map.GetComponent<MapComponent_BadgeyChaos>();
            if (comp != null && comp.IsActive) return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            var comp = map.GetComponent<MapComponent_BadgeyChaos>();

            if (comp == null) return false;

            // start chaos
            comp.TriggerChaos();

            // letter to player
            Find.LetterStack.ReceiveLetter(
                "Badgey Betrayal", 
                "The holographic instructor 'Badgey' has malfunctioned due to a buffer overflow! He is manipulating ship systems.\n\nQuickly! Use a Security Console to purge his program code before he vents the atmosphere!", 
                LetterDefOf.ThreatBig, 
                LookTargets.Invalid 
            );

            return true;
        }
    }
}