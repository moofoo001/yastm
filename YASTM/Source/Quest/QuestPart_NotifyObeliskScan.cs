using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace YASTM.QuestParts
{
    public class QuestPart_NotifyObeliskScan : QuestPart
    {
        public string inSignal;
        public string outSignal;
        public bool atA;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag != inSignal)
                return;

            try
            {
                var args = signal.args;  // struct, never null
                Map map = args.GetArg<Map>("SUBJECT")
                         ?? args.GetArg<Map>("map")
                         ?? args.GetArg<Thing>("SUBJECT")?.Map
                         ?? Find.CurrentMap;

                if (map != null)
                {
                    var comp = map.components?.FirstOrDefault(c => c?.GetType()?.Name == "MapComponent_ObeliskFlow");
                    if (comp != null)
                    {
                        var t = comp.GetType();
                        var m = t.GetMethod("RegisterObeliskScan", new[] { typeof(bool) })
                             ?? t.GetMethod("RegisterScan",        new[] { typeof(bool) })
                             ?? t.GetMethod("RegisterScan",        Type.EmptyTypes);

                        if (m != null)
                        {
                            if (m.GetParameters().Length == 1) m.Invoke(comp, new object[] { atA });
                            else                                m.Invoke(comp, null);
                        }
                        else
                        {
                            Log.Warning("[YASTM] ObeliskFlow found but no Register* method was available.");
                        }
                    }
                    else
                    {
                        Log.Warning("[YASTM] MapComponent_ObeliskFlow not found on map.");
                    }
                }
                else
                {
                    Log.Warning("[YASTM] No map available in QuestPart_NotifyObeliskScan.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[YASTM] QuestPart_NotifyObeliskScan failed: {ex}");
            }

            if (!outSignal.NullOrEmpty())
                Find.SignalManager.SendSignal(new Signal(outSignal, signal.args));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref outSignal, "outSignal");
            Scribe_Values.Look(ref atA, "atA", false);
        }
    }
}
