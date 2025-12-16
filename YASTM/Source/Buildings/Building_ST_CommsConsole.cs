using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class Building_ST_CommsConsole : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {

            foreach (var g in base.GetGizmos())
                yield return g;


            var link = GetComp<CompConsoleLink>();
            string reason = null;
            bool linked = link != null && link.HasLink(out reason);

            var cmdOpen = new Command_Action
            {
                defaultLabel = "Open communication",
                defaultDesc  = linked
                    ? "Contact Starfleet via the linked comm beacon."
                    : $"Needs a linked comm beacon: {reason ?? "no partner found"}.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CallInSupply", false),
                action = () =>
                {
                    if (!linked) return;
                    Messages.Message("Subspace channel open.", MessageTypeDefOf.PositiveEvent);

                }
            };
            if (!linked) cmdOpen.Disable(reason ?? "No linked beacon.");

            yield return cmdOpen;


        }
    }
}

