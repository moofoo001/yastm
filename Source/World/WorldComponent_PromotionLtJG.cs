using RimWorld;
using RimWorld.Planet;   // <-- wichtig: WorldComponent, World
using Verse;

namespace YASTM
{
    public class WorldComponent_PromotionLtJG : WorldComponent
    {
        public bool Active;
        public bool ScanDone;
        public bool SweepDone;
        public bool DiploDone;
        public bool RewardGiven;

        public WorldComponent_PromotionLtJG(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Active,      "STQ_LJG_Active",      false);
            Scribe_Values.Look(ref ScanDone,    "STQ_LJG_ScanDone",    false);
            Scribe_Values.Look(ref SweepDone,   "STQ_LJG_SweepDone",   false);
            Scribe_Values.Look(ref DiploDone,   "STQ_LJG_DiploDone",   false);
            Scribe_Values.Look(ref RewardGiven, "STQ_LJG_RewardGiven", false);
        }

        public void Start()
        {
            Active = true;
                    Find.LetterStack.ReceiveLetter(
            "STQ.LJG.Alert.Label".Translate(),
            "STQ.LJG.Alert.Desc".Translate() + "\n\n" +
            "• " + "STQ.LJG.Task.Scan".Translate()  + "\n" +
            "• " + "STQ.LJG.Task.Sweep".Translate() + "\n" +
            "• " + "STQ.LJG.Task.Diplo".Translate(),
            LetterDefOf.NeutralEvent);
            ScanDone = SweepDone = DiploDone = false;
            RewardGiven = false;
            Messages.Message("STQ.LJG.Start".Translate(), MessageTypeDefOf.NeutralEvent);
        }

        public void NotifyScanCompleted()  { if (Active && !ScanDone)  { ScanDone  = true; TryComplete(); } }
        public void NotifySweepCompleted() { if (Active && !SweepDone) { SweepDone = true; TryComplete(); } }
        public void NotifyDiploCompleted() { if (Active && !DiploDone) { DiploDone = true; TryComplete(); } }

        private void TryComplete()
        {
            if (!Active || RewardGiven) return;
            if (!(ScanDone && SweepDone && DiploDone)) return;

            RewardGiven = true;
            Active = false;

            var map = Find.AnyPlayerHomeMap;
            if (map != null)
            {
                IntVec3 cell = DropCellFinder.TradeDropSpot(map);
                var pipDef = DefDatabase<ThingDef>.GetNamedSilentFail("ST_PromotionPip_LJG");
                if (pipDef != null)
                {
                    var pip = ThingMaker.MakeThing(pipDef);
                    GenPlace.TryPlaceThing(pip, cell, map, ThingPlaceMode.Near);
                }
                Messages.Message("STQ.LJG.Complete".Translate(),
                    new LookTargets(cell, map), MessageTypeDefOf.PositiveEvent);
                Find.LetterStack.ReceiveLetter(
                    "STQ.LJG.Complete".Translate(),
                    "STQ.LJG.Complete".Translate(),
                    LetterDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("STQ.LJG.Complete".Translate(), MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
