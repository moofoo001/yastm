using System;
using RimWorld;
using Verse;
using YASTM;
using YASTM.MapSystems;


namespace YASTM.Comps
{
    public class CompProperties_ScanWork : CompProperties
    {
        public int workRequired = 4000;

        public CompProperties_ScanWork()
        {
            compClass = typeof(CompScanWork);
        }
    }

 
    public class CompScanWork : ThingComp
    {
        public CompProperties_ScanWork Props => (CompProperties_ScanWork)props;

        private int workDone;

        public bool IsComplete => workDone >= Props.workRequired;

        public void AddWork(int amount)
        {
            workDone = Math.Min(Props.workRequired, workDone + Math.Max(0, amount));
        }

        public void RegisterIfComplete(bool atA)
        {
            if (!IsComplete) return;
            var flow = parent.Map?.GetComponent<MapComponent_ObeliskFlow>();
            flow?.RegisterScan(atA);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref workDone, "workDone", 0);
        }
    }
}

