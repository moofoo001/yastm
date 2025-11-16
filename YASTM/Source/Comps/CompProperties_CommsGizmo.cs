using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    public class CompProperties_CommsGizmo : CompProperties
    {
        // XML: <aidCooldownDays>…</aidCooldownDays>
        public int aidCooldownDays = 7;

        // XML: <contactCooldownDays>…</contactCooldownDays>
        public int contactCooldownDays = 7;

        // XML: <requiredRankTraits><li>…</li></requiredRankTraits>
        public List<TraitDef> requiredRankTraits = new List<TraitDef>();

        // Optional: wie viele ST-Quests dürfen gleichzeitig aktiv sein?
        // XML: <maxActiveStarfleetQuests>2</maxActiveStarfleetQuests>
        public int maxActiveStarfleetQuests = 2;

        // Optional: wie erkennen wir "Starfleet"-Quests?
        // XML: <questPrefix>STQ_</questPrefix>
        public string questPrefix = "STQ_";

        public CompProperties_CommsGizmo()
        {
            compClass = typeof(CompCommsGizmo);
        }
    }
}
