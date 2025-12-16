using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    public static class TransporterPadUtil
    {
      
        public static bool TryGetLinkedPad(Building console, out Building pad)
        {
            pad = null;
            if (console == null) return false;

            var caf = console.GetComp<CompAffectedByFacilities>();
            if (caf == null) return false;

            foreach (var thing in caf.LinkedFacilitiesListForReading)
            {
                if (thing is not Building b) continue;
                if (b.def?.defName != "ST_TransporterPad") continue;

                var pw = b.GetComp<CompPowerTrader>();
                if (pw != null && !pw.PowerOn) continue;

                pad = b;
                return true;
            }
            return false;
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
            return (p == null || p.PowerOn) && (f == null || f.SwitchIsOn);
        }
    }
}

