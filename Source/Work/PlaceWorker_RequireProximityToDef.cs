using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine; // <-- FIX für Color

namespace YASTM
{
    public class ProximityRequirementExtension : DefModExtension
    {
        public List<string> targetDefs;   // z.B. { "ST_GravEngine" }
        // public int maxDistance = 15;      // Tiles
        public int maxDistance = YASTM_Mod.Settings?.transporterMaxRange ?? 15;
        public bool allowBlueprints = true;
    }

    public class PlaceWorker_RequireProximityToDef : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
                                                       Thing thingToIgnore = null, Thing thing = null)
        {
            var ext = checkingDef.GetModExtension<ProximityRequirementExtension>();
            if (ext == null || ext.targetDefs == null || ext.targetDefs.Count == 0)
                return true;

            var targets = GatherTargets(map, ext);
            if (targets.Count == 0)
                return "ST.Transporter.NearGravEngine.Required".Translate();

            int max = ext.maxDistance <= 0 ? 1 : ext.maxDistance;

            foreach (var t in targets)
            {
                var rect = GenAdj.OccupiedRect(t.Position, t.Rotation, t.def.Size);
                var closest = rect.ClosestCellTo(loc);
                if (closest.DistanceTo(loc) <= max)
                    return true;
            }
            return "ST.Transporter.NearGravEngine.TooFar".Translate(max);
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var ext = def.GetModExtension<ProximityRequirementExtension>();
            if (ext == null) return;

            var map = Find.CurrentMap;
            if (map == null) return;

            var targets = GatherTargets(map, ext);
            for (int i = 0; i < targets.Count; i++)
                GenDraw.DrawRadiusRing(targets[i].Position, ext.maxDistance);
        }

        private static List<Thing> GatherTargets(Map map, ProximityRequirementExtension ext)
        {
            var list = new List<Thing>();
            for (int i = 0; i < ext.targetDefs.Count; i++)
            {
                var td = DefDatabase<ThingDef>.GetNamedSilentFail(ext.targetDefs[i]);
                if (td == null) continue;

                list.AddRange(map.listerThings.ThingsOfDef(td));

                if (ext.allowBlueprints)
                {
                    var blueprints = map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint);
                    for (int b = 0; b < blueprints.Count; b++)
                    {
                        if (blueprints[b] is Blueprint_Build bp && bp.def.entityDefToBuild == td)
                            list.Add(bp);
                    }
                }
            }
            return list;
        }
    }
}
