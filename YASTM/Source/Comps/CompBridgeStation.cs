using Verse;
using RimWorld;
using System.Collections.Generic;

namespace YASTM.Source.Comps
{
    public enum BridgeRole
    {
        None,
        Command,    // Captain
        Tactical,   // Worf / Reed
        Ops,        // Data / Harry Kim
        Science,    // Spock / Jadzia Dax
        Helm        // Pilot / Paris / Mayweather
    }

    public class CompProperties_BridgeStation : CompProperties
    {
        public BridgeRole role = BridgeRole.None;
        public bool mannable = true;

        public CompProperties_BridgeStation()
        {
            this.compClass = typeof(CompBridgeStation);
        }
    }

    public class CompBridgeStation : ThingComp
    {
        public CompProperties_BridgeStation Props => (CompProperties_BridgeStation)props;

        public bool IsManned
        {
            get
            {
                // 1. Vanilla Check
                CompMannable mannable = parent.TryGetComp<CompMannable>();
                if (mannable != null && mannable.MannedNow) return true;

                // 2. Advanced Job Check
                if (parent is Building building && parent.Map != null)
                {

                    
                    List<IntVec3> cellsToCheck = new List<IntVec3>
                    {
                        building.InteractionCell,
                        building.Position
                    };

                    foreach (IntVec3 cell in cellsToCheck)
                    {
                        // Optimization: Check only if cell is valid
                        if (!cell.InBounds(parent.Map)) continue;

                        List<Thing> things = cell.GetThingList(parent.Map);
                        for (int i = 0; i < things.Count; i++)
                        {
                            if (things[i] is Pawn p && p.IsColonist)
                            {
                                // Verify the pawn is actually doing OUR job targeting THIS building
                                if (p.CurJob != null && 
                                    p.CurJob.def.defName == "ST_Job_ManBridgeStation" &&
                                    p.CurJob.targetA.Thing == parent)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}