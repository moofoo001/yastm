// Source/Incidents/IncidentWorker_StartPromotionObjectives.cs
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YASTM
{
    public class IncidentWorker_StartPromotionObjectives : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            var map = parms.target as Map;
            if (map == null) return false;

            var wc = Find.World.GetComponent<WorldComponent_PromotionObjectives>();
            if (wc == null) return false;

            return map.mapPawns.FreeColonistsSpawned.Any(p => wc.CanStartFor(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = parms.target as Map;
            var wc  = Find.World.GetComponent<WorldComponent_PromotionObjectives>();
            if (map == null || wc == null) return false;

            var candidates = map.mapPawns.FreeColonistsSpawned
                .Where(p => wc.CanStartFor(p))
                .ToList();

            if (candidates.Count == 0) return false;

            if (candidates.Count == 1)
                return wc.StartForPawn(candidates[0]);

          
            var root = new DiaNode("STQ.Promo.ChooseCandidate".Translate());
            foreach (var p in candidates)
            {
                var opt = new DiaOption(p.LabelShortCap);
                opt.action = () => wc.StartForPawn(p);
                opt.resolveTree = true;
                root.options.Add(opt);
            }
            root.options.Add(DiaOption.DefaultOK);

            Find.WindowStack.Add(new Dialog_NodeTree(root, true, false, "STQ.Promo.ChooseTitle".Translate()));
            return true;
        }
    }
}

