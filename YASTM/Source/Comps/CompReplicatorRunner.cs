using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace YASTM
{

    public class CompReplicatorRunner : ThingComp
    {
        private Building_WorkTable Table => parent as Building_WorkTable;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;
            if (Table?.BillStack == null) yield break;

            yield return new Command_Action
            {
                defaultLabel = "Run first bill now",
                defaultDesc  = "Pick a healthy colonist and start the first active bill at this replicator.",
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/Replicate", true),
                action       = SelectPawnAndRun
            };
        }

        private void SelectPawnAndRun()
        {
            var map = parent.Map;
            if (map == null) return;

        
            var bill = Table.BillStack.Bills.FirstOrDefault(b => b != null && !b.suspended);
            if (bill == null)
            {
                Messages.Message("No active bill found.", MessageTypeDefOf.RejectInput);
                return;
            }

            var candidates = map.mapPawns.FreeColonists
                .Where(p => !p.Dead && !p.Downed
                            && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                            && p.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                .ToList();

            if (candidates.Count == 0)
            {
                Messages.Message("No healthy colonist available.", MessageTypeDefOf.RejectInput);
                return;
            }

            var opts = new List<FloatMenuOption>();
            foreach (var p in candidates)
            {
                opts.Add(new FloatMenuOption(p.LabelShortCap, () =>
                {
                    var job = JobMaker.MakeJob(JobDefOf.DoBill, parent);
                    job.bill = bill;
                    p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(opts));
        }
    }
}

