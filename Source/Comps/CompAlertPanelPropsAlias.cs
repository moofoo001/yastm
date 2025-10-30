// Source/Comps/CompAlertPanelPropsAlias.cs
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    /// <summary>
    /// XML-Alias: Erlaubt Tick-basierte Eingaben im Building-Def
    /// und mappt sie auf die Sekunden-/Tick-Felder des eigentlichen Gizmo-Comps.
    /// </summary>
    public class CompProperties_AlertPanel : CompProperties_AlertPanelGizmo
    {
        // -- TICK-basierte Eingaben aus XML (nur hier definiert) --
        public int redCooldownTicks = 0;
        public int yellowCooldownTicks = 0;

        // Wichtig:
        // KEINE erneute Deklaration von redDurationTicks / yellowDurationTicks / blinkSeconds!
        // Diese Felder sind bereits in CompProperties_AlertPanelGizmo vorhanden
        // und werden direkt aus dem XML befüllt.

        public CompProperties_AlertPanel()
        {
            // Der tatsächliche Comp ist der Gizmo-Comp:
            compClass = typeof(CompAlertPanelGizmo);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            redCooldownSeconds    = Mathf.Max(0, Mathf.RoundToInt(redCooldownTicks    / 60f));
            yellowCooldownSeconds = Mathf.Max(0, Mathf.RoundToInt(yellowCooldownTicks / 60f));

            // Defaults aus Mod-Settings verwenden, falls XML 0 liefert
            var S = YASTM_Mod.Settings;

            if (redCooldownSeconds == 0)    redCooldownSeconds    = S?.defaultRedCooldownSeconds    ?? 0;
            if (yellowCooldownSeconds == 0) yellowCooldownSeconds = S?.defaultYellowCooldownSeconds ?? 0;

            if (redDurationTicks <= 0)      redDurationTicks      = S?.DefaultRedDurationTicks      ?? 0;
            if (yellowDurationTicks <= 0)   yellowDurationTicks   = S?.DefaultYellowDurationTicks   ?? 0;

            if (blinkSeconds <= 0)          blinkSeconds          = S?.defaultBlinkSeconds          ?? 0;
        }
    }
}
