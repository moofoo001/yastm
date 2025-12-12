using System.Collections.Generic;
using Verse;
using RimWorld;

namespace YASTM.Source.Comps
{
    public enum BridgeRole
    {
        None,
        Command,
        Tactical,
        Ops,
        Science,
        Conn
    }

    public class CompProperties_BridgeStation : CompProperties
    {
        public BridgeRole role = BridgeRole.None;

        public CompProperties_BridgeStation()
        {
            this.compClass = typeof(CompBridgeStation);
        }
    }

    public class CompBridgeStation : ThingComp
    {
        public CompProperties_BridgeStation Props => (CompProperties_BridgeStation)props;

        // Prüft, ob die Station aktuell aktiv besetzt ist
        public bool IsManned
        {
            get
            {
                // Nutzt Vanilla CompMannable
                CompMannable mannable = parent.TryGetComp<CompMannable>();
                if (mannable != null && mannable.MannedNow)
                {
                    return true;
                }
                
                // Fallback: Prüfen ob ein Pawn auf dem InteractionCell steht und arbeitet
                // (Für Gebäude ohne CompMannable aber mit Interaction)
                if (parent is Building building)
                {
                    // Einfache Logik: Ist ein Colonist auf dem Stuhl/Spot?
                    IntVec3 spot = parent.InteractionCell;
                    List<Thing> thingList = spot.GetThingList(parent.Map);
                    foreach (Thing t in thingList)
                    {
                        if (t is Pawn p && p.IsColonist && !p.Downed && !p.Drafted) 
                        {
                            // Strengere Logik: Führt er gerade einen Job an diesem Building aus?
                            if (p.CurJob != null && p.CurJob.targetA.Thing == parent)
                                return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}