// Source/Comps/CompAlertPanelPropsAlias.cs
using UnityEngine;
using Verse;

namespace YASTM
{
    /// <summary>
    /// Alias für alte XMLs: <li Class="YASTM.CompProperties_AlertPanel"> ... </li>
    /// Mappt Tick-Felder automatisch auf Sekunden-Felder der Basisklasse.
    /// </summary>
    public class CompProperties_AlertPanel : CompProperties_AlertPanelGizmo
    {
        // Legacy-Felder aus XML (Ticks)
        public int redDurationTicks     = -1;
        public int redCooldownTicks     = -1;
        public int yellowDurationTicks  = -1;
        public int yellowCooldownTicks  = -1;

        // Legacy-Flag (wird aktuell nicht zwingend benötigt, behalten für Zukunft)
        public bool playRedSirenLoop    = true;

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);

            // Ticks -> Sekunden übertragen, wenn in XML gesetzt
            if (redCooldownTicks >= 0)
                redCooldownSeconds = Mathf.Max(1, Mathf.RoundToInt(redCooldownTicks / 60f));

            if (yellowCooldownTicks >= 0)
                yellowCooldownSeconds = Mathf.Max(1, Mathf.RoundToInt(yellowCooldownTicks / 60f));


        }
    }
}
