using RimWorld;
using Verse;
using RimWorld.Planet;

namespace YASTM
{
    public class Alert_PromotionLtJGProgress : Alert
    {
        public Alert_PromotionLtJGProgress()
        {
            defaultLabel = "STQ.LJG.Alert.Label".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            var wc = Find.World.GetComponent<WorldComponent_PromotionLtJG>();
            return (wc != null && wc.Active && !wc.RewardGiven)
                ? AlertReport.Active
                : AlertReport.Inactive;
        }

        // <-- Rückgabetyp: TaggedString (nicht string)
        public override TaggedString GetExplanation()
        {
            var wc = Find.World.GetComponent<WorldComponent_PromotionLtJG>();
            if (wc == null || !wc.Active || wc.RewardGiven) return TaggedString.Empty;

            string exp = "STQ.LJG.Alert.Desc".Translate() + "\n\n";
            exp += Format("STQ.LJG.Task.Scan".Translate(),  wc.ScanDone);
            exp += Format("STQ.LJG.Task.Sweep".Translate(), wc.SweepDone);
            exp += Format("STQ.LJG.Task.Diplo".Translate(), wc.DiploDone);
            return exp; // implizit zu TaggedString
        }

        private static string Format(string label, bool done)
            => (done ? "[x] " : "[ ] ") + label + "\n";
    }
}
