using System.Linq;
using Verse;

namespace StarTrekFactions.Debug
{
    [StaticConstructorOnStartup]
    public static class YASTM_DefAudit
    {
        static YASTM_DefAudit()
        {
            // Welche Beacon-Defs sind wirklich geladen?
            var beacons = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.defName == "ST_ComBeacon").ToList();

            if (beacons.Count == 0)
                Log.Warning("[YASTM][AUDIT] No ThingDef 'ST_ComBeacon' found!");
            else
            {
                foreach (var b in beacons)
                {
                    var comps = b.comps?.Select(c => c.GetType().FullName)
                               ?? Enumerable.Empty<string>();
                    Log.Message($"[YASTM][AUDIT] Beacon from '{b.modContentPack?.Name}' " +
                                $"comps=[{string.Join(", ", comps)}]");
                }
            }

            // Gleiches für Sensor (optional)
            var sensors = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.defName == "ST_AnomalySensor").ToList();

            foreach (var s in sensors)
            {
                var comps = s.comps?.Select(c => c.GetType().FullName)
                           ?? Enumerable.Empty<string>();
                Log.Message($"[YASTM][AUDIT] Sensor from '{s.modContentPack?.Name}' " +
                            $"comps=[{string.Join(", ", comps)}]");
            }
        }
    }
}
