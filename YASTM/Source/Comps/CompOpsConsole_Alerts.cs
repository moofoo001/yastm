using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    // Dieser Teil der Klasse kümmert sich um die Buttons an der Konsole
    public partial class CompOpsConsole : ThingComp
    {
        // 1. Die Logik (Schaltet den Alarm um)
        private void ToggleRedAlert()
        {
            var mapComp = parent.Map.GetComponent<ST_ColonyAlertSystem>();
            if (mapComp == null) return;

            if (mapComp.CurrentLevel == ST_AlertLevel.Red)
                mapComp.SetAlertLevel(ST_AlertLevel.Normal);
            else
                mapComp.SetAlertLevel(ST_AlertLevel.Red);
        }

        private void ToggleYellowAlert()
        {
            var mapComp = parent.Map.GetComponent<ST_ColonyAlertSystem>();
            if (mapComp == null) return;

            if (mapComp.CurrentLevel == ST_AlertLevel.Yellow)
                mapComp.SetAlertLevel(ST_AlertLevel.Normal);
            else
                mapComp.SetAlertLevel(ST_AlertLevel.Yellow);
        }
        
        // Status-Prüfung für die Buttons
        public bool IsRedAlert => parent.Map.GetComponent<ST_ColonyAlertSystem>()?.CurrentLevel == ST_AlertLevel.Red;
        public bool IsYellowAlert => parent.Map.GetComponent<ST_ColonyAlertSystem>()?.CurrentLevel == ST_AlertLevel.Yellow;

        // 2. DIE GIZMOS (Hier kommen die Buttons zurück!)
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Immer erst die Basis-Gizmos holen (falls vorhanden)
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            // --- RED ALERT BUTTON ---
            Command_Toggle cmdRed = new Command_Toggle();
            cmdRed.defaultLabel = "Red Alert";
            cmdRed.defaultDesc = "Raise shields and sound the alarm! (Click to toggle)";
            cmdRed.icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RedAlert", true);
            cmdRed.isActive = () => IsRedAlert; // Leuchtet, wenn aktiv
            cmdRed.toggleAction = ToggleRedAlert;
            yield return cmdRed;

            // --- YELLOW ALERT BUTTON ---
            Command_Toggle cmdYellow = new Command_Toggle();
            cmdYellow.defaultLabel = "Yellow Alert";
            cmdYellow.defaultDesc = "Caution status. (Click to toggle)";
            cmdYellow.icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/YellowAlert", true);
            cmdYellow.isActive = () => IsYellowAlert; // Leuchtet, wenn aktiv
            cmdYellow.toggleAction = ToggleYellowAlert;
            yield return cmdYellow;
        }
    }
}