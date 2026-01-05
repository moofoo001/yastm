using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    public static class TransporterPadUtil
    {
        // Altes Helper (kann bleiben für Abwärtskompatibilität, oder wir nutzen nur den neuen)
        public static bool TryGetLinkedPad(Building console, out Building pad)
        {
            pad = GetLinkedPads(console).FirstOrDefault();
            return pad != null;
        }

        // NEU: Gibt alle aktiven, verbundenen Pads zurück
        public static List<Building> GetLinkedPads(Building console, int maxCount = 5)
        {
            List<Building> pads = new List<Building>();
            if (console == null) return pads;

            var caf = console.GetComp<CompAffectedByFacilities>();
            if (caf == null) return pads;

            foreach (var thing in caf.LinkedFacilitiesListForReading)
            {
                // Limit check (z.B. max 5 Pads pro Konsole)
                if (pads.Count >= maxCount) break;

                if (thing is not Building b) continue;
                if (b.def?.defName != "ST_TransporterPad") continue;

                // Check Power & Breakdown
                if (!IsPoweredOn(b)) continue;

                pads.Add(b);
            }
            return pads;
        }

        public static IntVec3 GetPadCell(Building pad)
        {
            if (pad == null) return IntVec3.Invalid;
            return pad.InteractionCell.IsValid ? pad.InteractionCell : pad.Position;
        }

        public static bool IsPoweredOn(Thing t)
        {
            var p = t.TryGetComp<CompPowerTrader>();
            var f = t.TryGetComp<CompFlickable>();
            var b = t.TryGetComp<CompBreakdownable>();
            
            bool power = (p == null || p.PowerOn);
            bool flick = (f == null || f.SwitchIsOn);
            bool broken = (b != null && b.BrokenDown);

            return power && flick && !broken;
        }
    }
}