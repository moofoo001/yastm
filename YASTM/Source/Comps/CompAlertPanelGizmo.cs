using UnityEngine;
using Verse;

namespace YASTM
{
    // Leitet alles an die neue Master-Klasse weiter
    public class CompProperties_AlertPanelGizmo : CompProperties_AlertPanel
    {
        public CompProperties_AlertPanelGizmo()
        {
            compClass = typeof(CompAlertPanel); 
        }
    }

    public class CompAlertPanelGizmo : CompAlertPanel
    {
        //  erbt Grafik und Gizmos vom Master
    }
}