using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace YASTM
{
    // ---- Extension, die an PreceptDefs hängt ----
    public class ST_VeneratedAnimalsExtension : DefModExtension
    {
        public List<string> animals;          // ThingDef.defName
        public float auraRadius = 8f;         // Reichweite
        public string presenceThoughtDef = "ST_VeneratedAnimal_Presence";
    }

    public static class VeneratedAnimalsUtility
    {
        // Liefert alle (Animals, Radius, Thought) Tripel aus den Precepts einer Ideo
        public static IEnumerable<(HashSet<ThingDef> animals, float radius, ThoughtDef thought)> EnumerateVeneration(Ideo ideo)
        {
            if (ideo == null) yield break;

            foreach (var precept in ideo.PreceptsListForReading)
            {
                var ext = precept?.def?.GetModExtension<ST_VeneratedAnimalsExtension>();
                if (ext == null) continue;

                var set = new HashSet<ThingDef>();
                if (ext.animals != null)
                {
                    foreach (var dn in ext.animals)
                    {
                        var td = DefDatabase<ThingDef>.GetNamedSilentFail(dn);
                        if (td != null) set.Add(td);
                    }
                }

                if (set.Count == 0) continue;

                var thought = DefDatabase<ThoughtDef>.GetNamedSilentFail(ext.presenceThoughtDef ?? "ST_VeneratedAnimal_Presence");
                float radius = ext.auraRadius > 0 ? ext.auraRadius : 8f;

                yield return (set, radius, thought);
            }
        }
    }

    // ---- Vergibt regelmäßig den Präsenz-Gedanken in Aura-Reichweite ----
    public class MapComponent_VeneratedAnimalsAura : MapComponent
    {
        private int nextTick;

        public MapComponent_VeneratedAnimalsAura(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick < nextTick) return;
            nextTick = tick + 250; // ~4s

            var colonists = map.mapPawns?.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0) return;

            foreach (var colonist in colonists)
            {
                var ideo = colonist?.Ideo;
                if (ideo == null) continue;

                foreach (var triple in VeneratedAnimalsUtility.EnumerateVeneration(ideo))
                {
                    if (triple.thought == null) continue;

                    if (AnyAnimalInRadius(colonist, triple.animals, triple.radius))
                    {
                        colonist.needs?.mood?.thoughts?.memories?
                            .TryGainMemory(triple.thought);
                        break; // einmal reicht
                    }
                }
            }
        }

        private static bool AnyAnimalInRadius(Pawn observer, HashSet<ThingDef> animals, float radius)
        {
            var map = observer.Map;
            if (map == null) return false;

            foreach (var c in GenRadial.RadialCellsAround(observer.Position, radius, true))
            {
                if (!c.InBounds(map)) continue;
                var list = c.GetThingList(map);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is Pawn p && p.Spawned && !p.Dead && animals.Contains(p.def))
                        return true;
                }
            }
            return false;
        }
    }
}
