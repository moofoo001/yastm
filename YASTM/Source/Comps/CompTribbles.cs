using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace YASTM
{
    public class CompProperties_Tribbles : CompProperties
    {
        public float purrRadius = 7f;
        public int overrunCountRoom = 12;
        public float reproMTBHours = 12f;
        public int maxNearby = 30;

        public CompProperties_Tribbles()
        {
            compClass = typeof(CompTribbles);
        }
    }

    public class CompTribbles : ThingComp
    {
        private int nextTick;
        public CompProperties_Tribbles Props => (CompProperties_Tribbles)props;
        private Pawn Pawn => parent as Pawn;

        public override void CompTickRare()
        {
            if (Pawn == null || Pawn.Map == null) return;
            int tick = Find.TickManager.TicksGame;
            if (tick < nextTick) return;
            nextTick = tick + 250;

            TryPurrAura();
            TryAsexualReproduce();
        }

        private void TryPurrAura()
        {
            if (!Pawn.Spawned) return;
            var map = Pawn.Map;
            var center = Pawn.Position;
            float rad = Props.purrRadius;

            int tribblesInRoom = 0;
            Room room = center.GetRoom(map);
            if (room != null)
                tribblesInRoom = room.ContainedAndAdjacentThings.Count(t => t is Pawn p && p.def == Pawn.def);

            var cells = GenRadial.RadialCellsAround(center, rad, true);
            foreach (var c in cells)
            {
                if (!c.InBounds(map)) continue;
                var p = c.GetFirstPawn(map);
                if (p != null && p.RaceProps.Humanlike && p.needs?.mood != null && p.Faction == Faction.OfPlayer)
                    p.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("ST_Tribbles_Purr"));
            }

            if (tribblesInRoom >= Props.overrunCountRoom && room != null)
            {
                foreach (var p in room.ContainedAndAdjacentThings.OfType<Pawn>().Where(pp => pp.RaceProps.Humanlike && pp.needs?.mood != null))
                    p.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("ST_Tribbles_Overrun"));
            }
        }

        private void TryAsexualReproduce()
        {
            var map = Pawn.Map;
            if (map == null) return;

            int nearby = GenRadial.RadialCellsAround(Pawn.Position, 12f, true)
                .Sum(c => c.InBounds(map) ? c.GetThingList(map).Count(t => t is Pawn p && p.def == Pawn.def) : 0);
            if (nearby >= Props.maxNearby) return;

            if (!Rand.MTBEventOccurs(Props.reproMTBHours, GenDate.HoursPerDay, 250)) return;

            var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("ST_Animal_Tribble_Kind");
            if (kind == null) return;

            var baby = PawnGenerator.GeneratePawn(kind, Pawn.Faction);
            if (baby == null) return;

            if (baby.ageTracker != null)
            {
                baby.ageTracker.AgeBiologicalTicks = 0;
                baby.ageTracker.AgeChronologicalTicks = 0;
            }

            GenSpawn.Spawn(baby, CellFinder.StandableCellNear(Pawn.Position, map, 1), map);
        }
    }
}

