using System.Collections.Generic;
using RimWorld;            // << important
using Verse;

namespace YASTM.Comps
{
    public class CompProperties_SubspaceScanner : CompProperties
    {
        public int workTicks = 1800;
        public CompProperties_SubspaceScanner() => compClass = typeof(CompSubspaceScanner);
    }

    public class CompSubspaceScanner : ThingComp
    {
        public CompProperties_SubspaceScanner Props => (CompProperties_SubspaceScanner)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "Initiate Scan",
                defaultDesc = "Operate the science console to scan the subspace anomaly.",
                icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Icons/Gizmos/ScienceScan"),
                action = () =>
                {
                    Messages.Message("Scanning can be started from the Science Console.", MessageTypeDefOf.RejectInput);
                }
            };
        }
    }
}

