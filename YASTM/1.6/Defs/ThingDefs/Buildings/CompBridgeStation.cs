using Verse;
using RimWorld;

namespace YASTM.Source.Comps
{
    // Definiert die Rolle der Station auf der Brücke
    public enum BridgeRole
    {
        None,
        Command,    // Captain
        Tactical,   // Worf / Reed
        Ops,        // Data / Harry Kim
        Science,    // Spock / Jadzia Dax
        Conn        // Pilot / Paris / Mayweather
    }

    public class CompProperties_BridgeStation : CompProperties
    {
        public BridgeRole role = BridgeRole.None;
        // Ob diese Station aktiv "bemannt" werden muss (WorkGiver) oder nur passiv (bei Benutzung) zählt
        public bool mannable = true; 

        public CompProperties_BridgeStation()
        {
            this.compClass = typeof(CompBridgeStation);
        }
    }

    public class CompBridgeStation : ThingComp
    {
        public CompProperties_BridgeStation Props => (CompProperties_BridgeStation)props;

        // Hilfsmethode für den Manager, um zu prüfen, ob hier gerade jemand arbeitet
        public bool IsManned
        {
            get
            {
                // Prüfung 1: Vanilla Mannable Comp (z.B. Turrets)
                CompMannable mannable = parent.TryGetComp<CompMannable>();
                if (mannable != null && mannable.MannedNow) return true;

                // Prüfung 2: Sitzt jemand drauf und führt unseren Job aus?
                if (parent is Building building && parent.Map != null)
                {
                    // Wir prüfen die InteractionCell
                    IntVec3 cell = building.InteractionCell;
                    var things = cell.GetThingList(parent.Map);
                    for (int i = 0; i < things.Count; i++)
                    {
                        if (things[i] is Pawn p && p.IsColonist)
                        {
                            // Prüfen, ob der Pawn den spezifischen Bridge-Job macht
                            if (p.CurJob != null && p.CurJob.def.defName == "ST_Job_ManBridgeStation")
                                return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}