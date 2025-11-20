using Verse;
using RimWorld;
using YASTM.Comps;
using YASTM.MapSystems;

namespace YASTM
{
    
    // Old XML: <li Class="YASTM.CompProperties_CommsGizmo"/>
    public class CompProperties_CommsGizmo : YASTM.Comps.CompProperties_CommsGizmo
    {
        public CompProperties_CommsGizmo() : base() { }
    }

    // Old XML: <li Class="YASTM.CompProperties_SubspaceScanner"/>
    public class CompProperties_SubspaceScanner : YASTM.Comps.CompProperties_SubspaceScanner
    {
        public CompProperties_SubspaceScanner() : base() { }
    }

    // Some code referenced this name without the MapSystems namespace
    public class MapComponent_ObeliskFlow : YASTM.MapSystems.MapComponent_ObeliskFlow
    {
        public MapComponent_ObeliskFlow(Map map) : base(map) { }
    }
}

namespace StarTrekFactions.Comps
{
    // Old XML: <li Class="StarTrekFactions.Comps.CompProperties_ScanWork"/>
    public class CompProperties_ScanWork : YASTM.Comps.CompProperties_ScanWork
    {
        public CompProperties_ScanWork() : base() { }
    }
}
