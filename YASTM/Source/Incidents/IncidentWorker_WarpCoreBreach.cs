using RimWorld;
using Verse;
using Verse.Sound;
using System.Linq;

namespace YASTM
{
    public class IncidentWorker_WarpCoreBreach : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            // manual search for the core
            return map.listerBuildings.allBuildingsColonist.Any(b => 
                b.def.defName == "ST_WarpCore" && 
                b.TryGetComp<CompWarpCore>() != null && 
                b.GetComp<CompPowerTrader>() != null && 
                b.GetComp<CompPowerTrader>().PowerOn);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // manual search for the core
            Thing core = map.listerBuildings.allBuildingsColonist
                .Where(b => b.def.defName == "ST_WarpCore")
                .RandomElementWithFallback(null);
            
            if (core == null) return false;

            // safe access to Comp
            CompWarpCore comp = core.TryGetComp<CompWarpCore>();
            if (comp == null) return false;

            // start breach
            comp.StartBreach();

            // play sound
            if (ST_SoundDefOf.ST_Sound_RedAlert != null) 
                ST_SoundDefOf.ST_Sound_RedAlert.PlayOneShotOnCamera(map);
            
            SendStandardLetter(parms, new LookTargets(core));
            return true;
        }
    }
}