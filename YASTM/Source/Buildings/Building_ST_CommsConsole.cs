// File: Source/Buildings/Building_ST_CommsConsole.cs
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM
{
    public class Building_ST_CommsConsole : Building_CommsConsole
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;

            var link = GetComp<CompConsoleLink>();
            if (link == null) yield break;

            if (!link.HasLink())
            {
                var cmd = new Command_Action
                {
                    defaultLabel = "No beacon in range",
                    defaultDesc  = "Place an ST_CommsBeacon within link range so this console can operate long-range comms.",
                    icon         = TexCommand.CannotShoot
                };
                cmd.Disable("Requires ST_CommsBeacon nearby.");
                yield return cmd;
            }
        }
    }
}
