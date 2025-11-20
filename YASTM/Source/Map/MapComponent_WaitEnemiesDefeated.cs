using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StarTrekFactions.QuestParts
{
    public class MapComponent_WaitEnemiesDefeated : MapComponent
    {
        private struct Entry
        {
            public int questId;
            public string outSignal;
            public bool onlyManhunters;
        }

        private readonly List<Entry> entries = new List<Entry>();

        public MapComponent_WaitEnemiesDefeated(Map map) : base(map) { }


        public static MapComponent_WaitEnemiesDefeated For(Map map)
        {
            var comp = map.GetComponent<MapComponent_WaitEnemiesDefeated>();
            if (comp == null)
            {
                comp = new MapComponent_WaitEnemiesDefeated(map);
                map.components.Add(comp);
            }
            return comp;
        }

        public void Arm(int questId, string outSignal, bool onlyManhunters)
        {
 
            entries.RemoveAll(e => e.questId == questId && e.outSignal == outSignal);
            entries.Add(new Entry { questId = questId, outSignal = outSignal, onlyManhunters = onlyManhunters });

            Log.Message($"[YASTM][WAIT/MC] armed on map='{map}' onlyManhunters={onlyManhunters} out='{outSignal}' quest={questId}");
        }

        public override void MapComponentTick()
        {
            if (entries.Count == 0) return;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];

                if (!HasBlockingEnemies(map, e.onlyManhunters, out int active, out int downed))
                {
                    string send = $"Quest{e.questId}.{e.outSignal}";
                    Log.Message($"[YASTM][WAIT/MC] cleared -> send '{send}' (active=0, downed={downed})");
                    Find.SignalManager.SendSignal(new Signal(send));
                    entries.RemoveAt(i);
                }
                else
                {

                    if (Find.TickManager.TicksGame % 300 == 0)
                        Log.Message($"[YASTM][WAIT/MC] still blocking (active={active}, downed={downed}) on map '{map}'.");
                }
            }
        }

        private static bool HasBlockingEnemies(Map map, bool onlyManhunters, out int active, out int downed)
        {
            active = 0; downed = 0;
            if (map == null) return false;

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                var p = pawns[i];
                if (!p.Spawned || p.Dead) continue;

                if (onlyManhunters)
                {
                    if (p.RaceProps?.Animal != true) continue;
                    var ms = p.MentalStateDef;
                    bool isMH = ms == MentalStateDefOf.Manhunter || ms == MentalStateDefOf.ManhunterPermanent;
                    if (!isMH) continue;

                    if (p.Downed) { downed++; continue; } 
                    active++;                              
                }
                else
                {
                    if (p.HostileTo(Faction.OfPlayer) && !p.Downed) active++;
                }
            }
            return active > 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();

        }
    }
}

