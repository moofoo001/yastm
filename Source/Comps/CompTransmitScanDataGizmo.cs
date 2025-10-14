using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StarTrekFactions.Comps
{
    public class CompProperties_TransmitScanDataGizmo : CompProperties
    {
        public string useLabel = "Transmit scan data";
        public string outSignal = "STQ.Obelisks.DataTransmitted";
        public bool requirePowerOn = true;
        public CompProperties_TransmitScanDataGizmo() { compClass = typeof(CompTransmitScanDataGizmo); }
    }

    public class CompTransmitScanDataGizmo : ThingComp
    {
        private bool sent;
        public CompProperties_TransmitScanDataGizmo Props => (CompProperties_TransmitScanDataGizmo)props;

        public override void PostExposeData() => Scribe_Values.Look(ref sent, "sent");

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Haupt-Command
            var cmd = new Command_Action
            {
                defaultLabel = Props.useLabel ?? "Transmit scan data",
                defaultDesc  = "Beam the obelisk telemetry to Starfleet.",
                action       = OnClick
            };

            string reason = null;

            if (sent) reason = "Already transmitted.";

            CompPowerTrader p = null;
            if (Props.requirePowerOn)
            {
                p = parent.TryGetComp<CompPowerTrader>();
                if (p != null && !p.PowerOn) reason = "Requires power.";
            }

            if (reason != null)
            {
                cmd.Disable(reason);
                Log.Message($"[YASTM][TRANSMIT] gizmo disabled → reason='{reason}', hasPowerComp={(p!=null)}, powerOn={(p?.PowerOn).GetValueOrDefault()}, sent={sent}");
            }

            yield return cmd;

            // DEV-Helfer immer anbieten, ignoriert alle Checks
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Force transmit (ignore checks)",
                    defaultDesc  = "Send 'STQ.Obelisks.DataTransmitted' ignoring all conditions.",
                    action       = () =>
                    {
                        Log.Warning("[YASTM][DEV] Force transmit used.");
                        DoTransmit();
                    }
                };
            }
        }

        private void OnClick()
        {
            if (sent) return;

            if (Props.requirePowerOn)
            {
                var p = parent.TryGetComp<CompPowerTrader>();
                if (p != null && !p.PowerOn)
                {
                    Messages.Message("Needs power", parent, MessageTypeDefOf.RejectInput);
                    return;
                }
            }

            DoTransmit();
        }

            private void DoTransmit()
            {
                var raw = Props.outSignal ?? "STQ.Obelisks.DataTransmitted";
                var subject = parent.Named("SUBJECT");

                // 1) Globales Signal (wie bisher)
                Find.SignalManager.SendSignal(new Signal(raw, subject));

                // 2) Zusätzlich: für JEDEN aktiven Quest das gescopte Signal schicken
                int scoped = 0;
                foreach (var q in Find.QuestManager.QuestsListForReading)
                {
                    // RimWorld nutzt "Quest{ID}." als Präfix (siehe Logzeilen "Quest0.…")
                    var withQuest = $"Quest{q.id}.{raw}";
                    Find.SignalManager.SendSignal(new Signal(withQuest, subject));
                    scoped++;
                }

                Log.Message($"[YASTM][TRANSMIT] broadcast '{raw}' + quest-scoped to {scoped} quests.");
                Messages.Message("Scan data transmitted.", parent, MessageTypeDefOf.PositiveEvent);
                sent = true;
            }
    }
}
