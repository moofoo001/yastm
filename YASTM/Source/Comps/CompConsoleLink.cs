using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class CompProperties_ConsoleLink : CompProperties
    {

        public List<string> linkTags = new List<string>();

        public string linkTag;    
        public List<string> tags;   


        public int maxLinkDistance = 25;

        public CompProperties_ConsoleLink()
        {
            compClass = typeof(CompConsoleLink);
        }


        public override void ResolveReferences(ThingDef def)
        {
            base.ResolveReferences(def);


            if (!string.IsNullOrEmpty(linkTag))
            {
                if (!linkTags.Contains(linkTag)) linkTags.Add(linkTag);
                linkTag = null;
            }

            if (tags != null && tags.Count > 0)
            {
                foreach (var t in tags)
                {
                    if (!string.IsNullOrEmpty(t) && !linkTags.Contains(t))
                        linkTags.Add(t);
                }
                tags = null;
            }


            linkTags = linkTags.Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
        }
    }

    public class CompConsoleLink : ThingComp
    {
        public CompProperties_ConsoleLink Props => (CompProperties_ConsoleLink)props;

        IEnumerable<Building> AllColonistBuildings(Map map)
            => map?.listerBuildings?.allBuildingsColonist ?? Enumerable.Empty<Building>();


        static int CellDistance(IntVec3 a, IntVec3 b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dz = Mathf.Abs(a.z - b.z);
            return Mathf.Max(dx, dz);
        }

        public IEnumerable<Building> Linked()
        {
            var map = parent.Map;
            if (map == null) yield break;

            var myTags = Props.linkTags;
            if (myTags == null || myTags.Count == 0) yield break;

            if (parent is not Building me) yield break;

            foreach (var b in AllColonistBuildings(map))
            {
                if (b == me) continue;

                var other = b.GetComp<CompConsoleLink>();
                if (other == null) continue;

                var otherTags = other.Props.linkTags;
                if (otherTags == null || otherTags.Count == 0) continue;

                // share at least one tag?
                if (!otherTags.Any(t => myTags.Contains(t))) continue;

                // within range (int vs int)
                if (CellDistance(b.Position, me.Position) > Props.maxLinkDistance) continue;

                yield return b;
            }
        }

        public int CountLinked() => Linked().Count();

        public bool HasLink(out string reason)
        {
            if (Props.linkTags == null || Props.linkTags.Count == 0)
            {
                reason = "No linkTags defined.";
                return false;
            }
            if (parent?.Map == null)
            {
                reason = "Not spawned on a map.";
                return false;
            }
            if (parent is not Building)
            {
                reason = "Parent is not a building.";
                return false;
            }

            int count = CountLinked();
            if (count <= 0)
            {
                reason = $"No partner with any of [{string.Join(", ", Props.linkTags)}] within {Props.maxLinkDistance} tiles.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}

