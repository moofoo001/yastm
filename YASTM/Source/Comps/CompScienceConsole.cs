using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    // Simple status gizmo; LCARS research boost is handled via facilities/links in XML.
    public class CompScienceConsole : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var map = parent.Map;
            if (map == null || parent.Faction != Faction.OfPlayer) yield break;

            int linkedSensors = map.listerThings.AllThings
                .Count(t => t.def?.defName == "ST_AnomalySensor" && parent.Position.InHorDistOf(t.Position, 60f));

            yield return new Command_Action
            {
                defaultLabel = $"Linked scanners: {linkedSensors}",
                defaultDesc  = "Number of anomaly scanners in range of this console.",
                icon         = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true),
                action       = () =>
                    Messages.Message($"Science console: {linkedSensors} scanner(s) in range.",
                        MessageTypeDefOf.NeutralEvent, historical: false)
            };
        }
    }
}
