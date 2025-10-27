using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace YASTM
{
    public class Alert_PromotionObjectivesProgress : Alert
    {
        public Alert_PromotionObjectivesProgress()
        {
            defaultLabel = "STQ.Promo.Alert.Label".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            var wc = Find.World.GetComponent<WorldComponent_PromotionObjectives>();
            return (wc != null && wc.AnyActive) ? AlertReport.Active : AlertReport.Inactive;
        }

        public override TaggedString GetExplanation()
        {
            var wc = Find.World.GetComponent<WorldComponent_PromotionObjectives>();
            if (wc == null || !wc.AnyActive) return TaggedString.Empty;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("STQ.Promo.Alert.Desc".Translate());
            sb.AppendLine();

            foreach (var pr in wc.actives)
            {
                if (pr?.pawn == null || pr.rewardGiven) continue;
                var rankLabel = pr.targetRank?.label?.CapitalizeFirst() ?? "rank";
                sb.AppendLine($"{pr.pawn.LabelShortCap} → {rankLabel}:");
                sb.AppendLine($"[ {(pr.scans >= pr.reqScans ? "x" : " ")} ]  " + "STQ.Promo.Task.Scan".Translate(pr.scans, pr.reqScans));
                sb.AppendLine($"[ {(pr.sweeps >= pr.reqSweeps ? "x" : " ")} ]  " + "STQ.Promo.Task.Sweep".Translate(pr.sweeps, pr.reqSweeps));
                sb.AppendLine($"[ {(pr.diplomacies >= pr.reqDiplos ? "x" : " ")} ]  " + "STQ.Promo.Task.Diplo".Translate(pr.diplomacies, pr.reqDiplos));
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
