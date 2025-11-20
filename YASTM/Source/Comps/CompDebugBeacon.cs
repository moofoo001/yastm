// Source/Comps/CompDebugBeacon.cs
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StarTrekFactions.Comps
{
    public class CompProperties_DebugBeacon : CompProperties
    {
        public CompProperties_DebugBeacon() { compClass = typeof(CompDebugBeacon); }
    }

    public class CompDebugBeacon : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent?.Faction != Faction.OfPlayer) yield break;

            yield return new Command_Action { defaultLabel = "YASTM: Dump beacon state", action = Dump };
            yield return new Command_Action { defaultLabel = "YASTM: SEND ScanA.Completed", action = () => Send("STQ.Obelisks.ScanA.Completed") };
            yield return new Command_Action { defaultLabel = "YASTM: SEND ScanB.Completed", action = () => Send("STQ.Obelisks.ScanB.Completed") };
            yield return new Command_Action { defaultLabel = "YASTM: SEND DataTransmitted", action = () => Send("STQ.Obelisks.DataTransmitted") };
        }

        void Dump()
        {
            var comps = parent.AllComps?.Select(c => c.GetType().FullName).ToList() ?? new List<string>();
            var power = parent.TryGetComp<CompPowerTrader>();
            var tx    = parent.TryGetComp<StarTrekFactions.Comps.CompTransmitScanDataGizmo>();
            Verse.Log.Message("[YASTM][BEACON DUMP] " +
                $"{parent} comps=[\n  {string.Join("\n  ", comps)}\n] " +
                $"power={(power!=null ? power.PowerOn.ToString() : "no powerComp")} | TransmitComp={(tx!=null?"present":"MISSING")}");
        }

        void Send(string tag) => Find.SignalManager.SendSignal(new Signal(tag, parent.Named("SUBJECT")));
    }
}

