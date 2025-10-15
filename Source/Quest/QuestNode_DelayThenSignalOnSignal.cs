using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.QuestNodes
{
    public class QuestNode_DelayThenSignalOnSignal : QuestNode
    {
        public string inSignal;
        public string inSignalRaw;

        public int delayTicks = 60000;
        public int delayTicksMin = -1;
        public int delayTicksMax = -1;

        public string outSignal;
        public bool debug;

        protected override void RunInt()
        {
            string raw = inSignalRaw.NullOrEmpty() ? inSignal : inSignalRaw;
            int chosen = delayTicks;
            if (delayTicksMin >= 0 && delayTicksMax >= 0 && delayTicksMax >= delayTicksMin)
                chosen = Rand.RangeInclusive(delayTicksMin, delayTicksMax);

            var p = new QuestPart_DelayThenSignalOnSignal
            {
                inSignalRaw     = raw,
                inSignalScoped  = QuestGenUtility.HardcodedSignalWithQuestID(raw),
                delayTicks      = chosen,
                outSignalRaw    = outSignal,
                outSignalScoped = outSignal.NullOrEmpty() ? null : QuestGenUtility.HardcodedSignalWithQuestID(outSignal),
                debug           = debug
            };
            QuestGen.quest.AddPart(p);
        }

        protected override bool TestRunInt(Slate slate)
            => !(inSignal.NullOrEmpty() && inSignalRaw.NullOrEmpty()) && !outSignal.NullOrEmpty();
    }

    public class QuestPart_DelayThenSignalOnSignal : QuestPart
    {
        public string inSignalRaw, inSignalScoped;
        public int delayTicks;
        public string outSignalRaw, outSignalScoped;
        public bool debug;

        private bool active;
        private int targetTick = -1;   // absoluter Ziel-Tick

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == inSignalRaw || signal.tag == inSignalScoped)
            {
                active = true;
                targetTick = Find.TickManager.TicksGame + delayTicks;
                StarTrekFactions.GameComponent_QuestWatchers.Instance?.Register(this);
                if (debug)
                    Log.Message($"[YASTM][Delay] Registered on '{signal.tag}', delayTicks={delayTicks}, targetTick={targetTick}, now={Find.TickManager.TicksGame}");
            }
        }

        // Aufruf durch GameComponent; absolute Prüfung statt runterzählen
        public bool TickAndMaybeFire(int _ignored = 0)
        {
            if (!active) return false;

            int now = Find.TickManager.TicksGame;
            if (debug && (now % 6000 == 0 || now >= targetTick))
                Log.Message($"[YASTM][Delay] check now={now}, left={(targetTick - now)}");

            if (now < targetTick) return false;

            active = false;
            if (!outSignalRaw.NullOrEmpty())    Find.SignalManager.SendSignal(new Signal(outSignalRaw));
            if (!outSignalScoped.NullOrEmpty()) Find.SignalManager.SendSignal(new Signal(outSignalScoped));
            if (debug)
                Log.Message($"[YASTM][Delay] DONE -> sent '{outSignalRaw}' + '{outSignalScoped}' at now={now}");

            return true;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignalRaw, "inSignalRaw");
            Scribe_Values.Look(ref inSignalScoped, "inSignalScoped");
            Scribe_Values.Look(ref delayTicks, "delayTicks", 60000);
            Scribe_Values.Look(ref outSignalRaw, "outSignalRaw");
            Scribe_Values.Look(ref outSignalScoped, "outSignalScoped");
            Scribe_Values.Look(ref active, "active");
            Scribe_Values.Look(ref targetTick, "targetTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && active)
                StarTrekFactions.GameComponent_QuestWatchers.Instance?.Register(this);
        }
    }
}
