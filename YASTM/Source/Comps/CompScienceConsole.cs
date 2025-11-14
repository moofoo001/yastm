// File: Source/Comps/CompScienceConsole.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld; // <- wichtig für MessageTypeDefOf

namespace YASTM
{
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
                defaultDesc  = "Scanner in Reichweite dieser Konsole.",
                icon         = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true),
                action       = () =>
                    Messages.Message($"Science Console: {linkedSensors} Scanner in Reichweite.",
                        MessageTypeDefOf.NeutralEvent, historical: false)
            };
        }
    }
}
