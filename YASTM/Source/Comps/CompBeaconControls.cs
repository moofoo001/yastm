using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM.Comps
{
    public class CompProperties_BeaconControls : CompProperties
    {
        public CompProperties_BeaconControls()
        {
            compClass = typeof(CompBeaconControls);
        }
    }

    public class CompBeaconControls : ThingComp
    {
        public CompProperties_BeaconControls Props => (CompProperties_BeaconControls)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var map = parent?.Map;
            if (map == null) yield break;

            var flow = map.GetComponent<YASTM.MapSystems.MapComponent_ObeliskFlow>();
            if (flow == null) yield break;

            // Show a transmit button once both scans are done and not yet sent.
            if (flow.BothScanned && !flow.transmitted)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Transmit Scan Data",
                    defaultDesc  = $"Send obelisk scan data to Starfleet. ({flow.ScannedCount}/2 complete)",
                    icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ScienceScan"),
                    action       = () =>
                    {
                        flow.OnTransmit();
                        Messages.Message("Scan data transmitted.", MessageTypeDefOf.PositiveEvent);
                    }
                };
            }
        }
    }
}
