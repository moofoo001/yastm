using Verse;

namespace YASTM.MapSystems
{
    public class MapComponent_ObeliskFlow : MapComponent
    {
        public bool scanA;
        public bool scanB;
        public bool transmitted;

        public MapComponent_ObeliskFlow(Map map) : base(map) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref scanA, "scanA", false);
            Scribe_Values.Look(ref scanB, "scanB", false);
            Scribe_Values.Look(ref transmitted, "transmitted", false);
        }

        // --- Register scans ---------------------------------------------------

        // Legacy shim (keeps old call sites compiling)
        public void RegisterScan() => RegisterScan(true);

        // Preferred overload: true = Obelisk A, false = Obelisk B
        public void RegisterScan(bool atA)
        {
            if (atA) scanA = true;
            else     scanB = true;
        }

        // --- State & gating ---------------------------------------------------

        // How many obelisks have been scanned (0..2)
        public int ScannedCount => (scanA ? 1 : 0) + (scanB ? 1 : 0);

        public bool BothScanned => scanA && scanB;

        public bool CanTransmit() => BothScanned && !transmitted;

        public void OnTransmit() => transmitted = true;
    }
}
