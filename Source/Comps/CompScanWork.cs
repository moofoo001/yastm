using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

namespace StarTrekFactions.Comps
{
    public class CompProperties_ScanWork : CompProperties
    {
        
        public int workTicksBase = 1800;

        
        public string successSignal;

        
        public bool requiresPoweredSensor = true;

        
        public float sensorRadius = 10f;

        
        public SkillDef skill;

        public CompProperties_ScanWork()
        {
            compClass = typeof(CompScanWork);
        }
    }

    public class CompScanWork : ThingComp
    {
        public CompProperties_ScanWork Props => (CompProperties_ScanWork)props;

        private bool completed;
        public bool Completed => completed;

        public int progressTicks;

        
        public bool PoweredSensorNearby()
        {
            return PoweredSensorNearby(null);
        }

        
        public bool PoweredSensorNearby(Pawn pawn)
        {
            if (!Props.requiresPoweredSensor) return true;

            Map map = parent?.Map;
            if (map == null) return false;

            foreach (var c in GenRadial.RadialCellsAround(parent.Position, Props.sensorRadius, true))
            {
                if (!c.InBounds(map)) continue;
                var things = c.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    var t = things[i];
                    if (t.def?.defName != "ST_AnomalySensor") continue;

                    var p = t.TryGetComp<CompPowerTrader>();
                    if (p == null || p.PowerOn) return true;
                }
            }
            return false;
        }


        public void OnScanFinished(Pawn pawn)
        {
            if (completed) return;
            completed = true;


            var tags = new List<string>();
            if (!Props.successSignal.NullOrEmpty())
            {
                tags.Add(Props.successSignal.Trim());
            }
            else
            {
                string dn = parent?.def?.defName ?? "";
                if (dn.IndexOf("Obelisk_A", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || dn.EndsWith("_A", System.StringComparison.OrdinalIgnoreCase))
                    tags.Add("STQ.Obelisks.ScanA.Completed");
                else if (dn.IndexOf("Obelisk_B", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || dn.EndsWith("_B", System.StringComparison.OrdinalIgnoreCase))
                    tags.Add("STQ.Obelisks.ScanB.Completed");
            }

            if (tags.Count == 0)
            {
                Log.Warning($"[YASTM][SCAN] OnScanFinished on {parent} but no tag could be determined.");
                return;
            }


            foreach (var baseTag in tags.Distinct())
            {
                string raw = baseTag;
                string scoped = QuestGenUtility.HardcodedSignalWithQuestID(baseTag);

                Log.Message($"[YASTM][SCAN] finishing on {parent} -> send '{raw}' and '{scoped}'");
                Find.SignalManager.SendSignal(new Signal(raw, parent.Named("SUBJECT")));
                Find.SignalManager.SendSignal(new Signal(scoped, parent.Named("SUBJECT")));
            }
            
            var mc = parent.Map?.GetComponent<MapComponent_ObeliskFlow>();
                if (mc != null)
                {
                    if (tags.Any(t => t.EndsWith("ScanA.Completed"))) mc.scanA = true;
                    if (tags.Any(t => t.EndsWith("ScanB.Completed"))) mc.scanB = true;
                }

        }
    }
}
