// Source/Comps/CompUseEffect_SpawnFarAndSignal.cs
using System;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace StarTrekFactions.Comps
{
    public class CompProperties_UseEffect_SpawnFarAndSignal : CompProperties_UseEffect
    {
        public string inSignal;            // z.B. "STQ.Obelisks.BeaconActivated"
        public int minDist = 42;
        public int maxDist = 72;
        public bool outsideHome = true;
        public bool requirePowerOn = true;

        public ThingDef thingA;
        public ThingDef thingB;

        // Setup-Quest, die sicher laufen soll
        public string questDefToEnsure = "STQ_VE_Obelisks_I_II_Setup";

        public CompProperties_UseEffect_SpawnFarAndSignal()
        {
            compClass = typeof(CompUseEffect_SpawnFarAndSignal);
        }
    }

    public class CompUseEffect_SpawnFarAndSignal : CompUseEffect
    {
        private bool initialized; // einmal-Schutz

        public CompProperties_UseEffect_SpawnFarAndSignal Props
            => (CompProperties_UseEffect_SpawnFarAndSignal)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref initialized, "initialized");
        }

        public override void DoEffect(Pawn usedBy)
        {
            var map = parent.Map;
            Log.Message($"[YASTM] UseEffect START on {parent?.def?.defName} (map={(map!=null)}, powerReq={Props.requirePowerOn})");

            if (map == null)
            {
                Messages.Message("[YASTM] Beacon has no map.", MessageTypeDefOf.RejectInput);
                return;
            }

            // NEU: Einmal-Gate für die komplette Initialisierung
            if (initialized)
            {
                Messages.Message("Beacon already initialized.", parent, MessageTypeDefOf.NeutralEvent);
                return;
            }

            // Power-Gate (nur Hinweis)
            if (Props.requirePowerOn)
            {
                var power = parent.TryGetComp<CompPowerTrader>();
                var on = power != null && power.PowerOn;
                Log.Message($"[YASTM] Power check: comp={(power!=null)} on={on}");
                if (!on)
                {
                    Messages.Message("Needs power", parent, MessageTypeDefOf.RejectInput);
                    return;
                }
            }

            // Quest sicherstellen
            EnsureQuestRunningOnce();

            // Zellen finden + Spawns
            IntVec3 a; TryFindCell(map, out a);
            IntVec3 b; TryFindCell(map, out b, a, 18);

            int spawned = 0;
            if (Props.thingA != null && a.IsValid)
            {
                GenSpawn.Spawn(Props.thingA, a, map, WipeMode.Vanish); spawned++;
                Log.Message($"[YASTM] Spawn A @ {a} ({Props.thingA.defName})");
            }
            else Log.Warning("[YASTM] thingA missing or invalid cell.");

            if (Props.thingB != null && b.IsValid)
            {
                GenSpawn.Spawn(Props.thingB, b, map, WipeMode.Vanish); spawned++;
                Log.Message($"[YASTM] Spawn B @ {b} ({Props.thingB.defName})");
            }
            else Log.Warning("[YASTM] thingB missing or invalid cell.");

            // Signal an Quest schicken
            if (!string.IsNullOrEmpty(Props.inSignal))
            {
                Find.SignalManager.SendSignal(new Signal(Props.inSignal, parent.Named("SUBJECT")));
                Log.Message($"[YASTM] Signal sent: {Props.inSignal}");
            }
            else Log.Warning("[YASTM] No inSignal configured.");

            // NEU: Einmal-Gate setzen – erst NACH erfolgreicher Initialisierung
            initialized = true;

            Messages.Message($"Beacon initialized ({spawned} spawn(s))", parent, MessageTypeDefOf.PositiveEvent);
        }
        // ---------- hier war die Methode zu hoch/außerhalb platziert ----------
            private void EnsureQuestRunningOnce()
        {
            if (initialized) { Log.Message("[YASTM] Quest already ensured."); return; }

            // 1) Setup-Quest erzeugen (falls noch nicht da) + NAME & DESCRIPTION fest setzen
            var setupDefName = Props.questDefToEnsure ?? "STQ_VE_Obelisks_I_II_Setup";
            var setup = DefDatabase<QuestScriptDef>.GetNamedSilentFail(setupDefName);
            if (setup == null) { Log.Warning($"[YASTM] QuestScriptDef not found: {setupDefName}"); return; }

            var qm = Find.QuestManager;
            bool setupExists = qm.QuestsListForReading.Any(q => q?.root == setup && !q.dismissed);
            if (!setupExists)
            {
                var q = QuestUtility.GenerateQuestAndMakeAvailable(setup, new Slate());
                if (q != null)
                {
                    q.name = "Obelisk Survey — Phase I";
                    q.description =
                        "Locate and scan the two alien obelisks. Build and power the subspace sensor near each obelisk to enable scans.";

                    QuestUtility.SendLetterQuestAvailable(q);
                    Log.Message($"[YASTM] Quest generated: {setupDefName} (renamed to '{q.name}')");
                }
            }
            else
            {
                // Falls schon da: Namen & Beschreibung trotzdem korrigieren
                foreach (var q in qm.QuestsListForReading.Where(x => x?.root == setup))
                {
                    q.name = "Obelisk Survey — Phase I";
                    q.description =
                        "Locate and scan the two alien obelisks. Build and power the subspace sensor near each obelisk to enable scans.";
                    Log.Message("[YASTM] Renamed & re-described existing setup quest to 'Obelisk Survey — Phase I'");
                }
            }
            // 2) Diagnose: vorhandene Quests loggen (ohne Backslashes in der Interpolation)
            foreach (var q in qm.QuestsListForReading)
            {
                if (q == null) continue;
                var root = q.root?.defName ?? "<null>";
                var name = q.name ?? "";
                Log.Message($"[YASTM] Quest present: root={root} name=\"{name}\" dismissed={q.dismissed}");
            }

            // 3) Alle "Wardens"-Quests hart beenden (egal wie benannt)
            foreach (var q in qm.QuestsListForReading.ToArray())
            {
                try
                {
                    string root = q?.root?.defName ?? "";
                    string name = q?.name ?? "";
                    if (!string.IsNullOrEmpty(root) &&
                        root.IndexOf("Wardens", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.Warning($"[YASTM] Ending stray Wardens quest: root={root} name=\"{name}\"");
                        q.End(RimWorld.QuestEndOutcome.Fail, sendLetter: false);
                    }
                }
                catch (System.Exception e) { Log.Warning("[YASTM] Wardens cleanup error: " + e); }
            }

            initialized = true;
        }

        private bool TryFindCell(Map map, out IntVec3 cell, IntVec3 other = default, int minFromOther = 0)
        {
            IntVec3 center = map.Center;

            bool Validator(IntVec3 c)
            {
                if (!c.InBounds(map) || c.Fogged(map) || !c.Standable(map)) return false;
                int d = (int)c.DistanceTo(center);
                if (d < Props.minDist || d > Props.maxDist) return false;
                if (Props.outsideHome && map.areaManager?.Home != null && map.areaManager.Home[c]) return false;
                if (other.IsValid && minFromOther > 0 && c.DistanceTo(other) < minFromOther) return false;
                var ed = c.GetEdifice(map);
                if (ed != null && ed.def.passability == Traversability.Impassable) return false;
                return true;
            }

            bool ok = CellFinder.TryFindRandomCellNear(center, map, Props.maxDist, Validator, out cell);
            Log.Message($"[YASTM] TryFindCell -> {ok} ({cell})");
            return ok;
        }
    }
}
