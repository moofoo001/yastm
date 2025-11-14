using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YASTM
{
    public class CompProperties_ConsoleLink : CompProperties
    {
        public string linkTag = "CommsBeacon";

        // Kanonisch
        public int maxLinkDistance = 20;
        // Alias aus XML: <maxDistance>
        public int maxDistance = -1;

        public CompProperties_ConsoleLink()
        {
            compClass = typeof(CompConsoleLink);
        }
    }

    public class CompConsoleLink : ThingComp
    {
        public CompProperties_ConsoleLink Props => (CompProperties_ConsoleLink)props;

        private int Radius =>
            Props.maxLinkDistance > 0 ? Props.maxLinkDistance :
            (Props.maxDistance > 0 ? Props.maxDistance : 20);

        public bool HasLink() => LinkedBuildings().Any();
        public bool HasLink(Map _ignored) => HasLink();
        public bool HasLink(int _ignored) => HasLink();
        public bool HasLink(string tag) =>
            (string.IsNullOrEmpty(tag) || tag == Props.linkTag) && HasLink();
        public bool HasLink(out Building firstLinked)
        {
            firstLinked = LinkedBuildings().FirstOrDefault();
            return firstLinked != null;
        }

        public IEnumerable<Building> LinkedBuildings()
        {
            if (parent?.Map == null) yield break;

            var map = parent.Map;
            foreach (var cell in GenRadial.RadialCellsAround(parent.Position, Radius, true))
            {
                if (!cell.InBounds(map)) continue;
                var things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    var t = things[i];

                    // harter Match
                    if (t.def?.defName == "ST_CommsBeacon" && t is Building b1)
                    {
                        yield return b1;
                        continue;
                    }

                    // generischer Tag-Match
                    var otherLink = t.TryGetComp<CompConsoleLink>();
                    if (otherLink != null && otherLink != this && otherLink.Props.linkTag == this.Props.linkTag && t is Building b2)
                        yield return b2;
                }
            }
        }
    }
}
