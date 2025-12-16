// File: Source/Comps/CompAlertPanelPropsAlias.cs
using Verse;

namespace YASTM
{

    public class CompProperties_AlertPanel : CompProperties_AlertPanelGizmo
    {
        public int? redCooldownSeconds;
        public int? yellowCooldownSeconds;

        public CompProperties_AlertPanel()
        {

            compClass = typeof(CompAlertPanelGizmo);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);


            if (redCooldownSeconds.HasValue)
                redCooldownTicks = redCooldownSeconds.Value * 60;

            if (yellowCooldownSeconds.HasValue)
                yellowCooldownTicks = yellowCooldownSeconds.Value * 60;
        }
    }
}

