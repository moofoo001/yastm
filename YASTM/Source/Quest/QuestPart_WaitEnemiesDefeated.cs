using RimWorld;
using Verse;

namespace StarTrekFactions.QuestParts
{

    public class QuestPart_WaitEnemiesDefeated : QuestPart
    {
        public string inSignalEnableRaw;        
        public string outSignal;                  
        public bool   onlyManhunters = true;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            // accept raw + quest-scoped variants
            string scoped = $"Quest{quest.id}.{inSignalEnableRaw}";
            if (signal.tag != inSignalEnableRaw && signal.tag != scoped) return;

            var map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null)
            {
                Log.Warning("[YASTM][WAIT] arm failed: no map available.");
                return;
            }

            MapComponent_WaitEnemiesDefeated.For(map)
                .Arm(quest.id, outSignal ?? "STQ.Obelisks.II.Cleared", onlyManhunters);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignalEnableRaw, nameof(inSignalEnableRaw));
            Scribe_Values.Look(ref outSignal, nameof(outSignal));
            Scribe_Values.Look(ref onlyManhunters, nameof(onlyManhunters), true);
        }
    }
}

