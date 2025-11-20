// File: Source/Comps/CompAlertPanelPropsAlias.cs
using Verse;

namespace YASTM
{
    /// <summary>
    /// Alias-Props für alte XMLs, die noch *Seconds-Felder benutzen.
    /// Konvertiert Sekunden -> Ticks in ResolveReferences und nutzt die neue Gizmo-Comp.
    /// </summary>
    public class CompProperties_AlertPanel : CompProperties_AlertPanelGizmo
    {
        // Legacy-Felder aus alten XMLs (optional)
        public int? redCooldownSeconds;
        public int? yellowCooldownSeconds;

        public CompProperties_AlertPanel()
        {
            // Weiterhin dieselbe Comp wie die neuen Props
            compClass = typeof(CompAlertPanelGizmo);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);

            // Sekunden -> Ticks (60 Ticks ~ 1 Sekunde)
            if (redCooldownSeconds.HasValue)
                redCooldownTicks = redCooldownSeconds.Value * 60;

            if (yellowCooldownSeconds.HasValue)
                yellowCooldownTicks = yellowCooldownSeconds.Value * 60;
        }
    }
}

