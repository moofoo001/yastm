using Verse;
using RimWorld;

namespace YASTM
{
    // role definition
    public enum BridgeRole
    {
        None,
        Command,
        Helm,
        Ops,
        Tactical,
        Science,
        Engineering
    }

    public class CompProperties_BridgeStation : CompProperties
    {
        // for synergy
        public BridgeRole role = BridgeRole.None;
        
        // for access restriction (e.g. "ST_Trait_Security")
        public string requiredTraitDef = ""; 

        public CompProperties_BridgeStation()
        {
            this.compClass = typeof(CompBridgeStation);
        }
    }

    public class CompBridgeStation : ThingComp
    {
        public CompProperties_BridgeStation Props => (CompProperties_BridgeStation)props;

        // Helper property for the manager: Is the thing currently being used?
        public bool IsManned
        {
            get
            {
                if (parent.Map == null) return false;
                // Check if a pawn is currently interacting
                IntVec3 interactionCell = parent.InteractionCell;
                foreach (Pawn p in interactionCell.GetThingList(parent.Map).ConvertAll(t => t as Pawn))
                {
                    if (p != null && p.CurJob != null && p.CurJob.targetA.Thing == parent)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}