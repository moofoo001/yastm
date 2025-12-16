// Source/Comps/CompInitializeBeaconGizmo.cs
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace StarTrekFactions.Comps
{
    public class CompProperties_InitializeBeaconGizmo : CompProperties
    {
        public string useLabel = "Init Beacon • YASTM"; 
        public bool requirePowerOn = false;            
        public CompProperties_InitializeBeaconGizmo()
        {
            compClass = typeof(CompInitializeBeaconGizmo);
        }
    }

    public class CompInitializeBeaconGizmo : ThingComp
    {
        public CompProperties_InitializeBeaconGizmo Props => (CompProperties_InitializeBeaconGizmo)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent?.Faction != Faction.OfPlayer) yield break;


            yield return new Command_Action
            {
                defaultLabel = Props.useLabel,
                defaultDesc  = "Initialize the subspace relay and start the obelisk survey.",
                icon         = ContentFinder<UnityEngine.Texture2D>.Get("UI/Icons/Gizmos/PowerOn"), // ToDo: custom icon
                action       = OnClick
            };
        }

        private void OnClick()
        {
            var power = parent.TryGetComp<CompPowerTrader>();
            if (Props.requirePowerOn && power != null && !power.PowerOn)
            {
                Messages.Message("Needs power", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            int count = 0;
            foreach (var c in parent.AllComps)
                if (c is CompUseEffect eff) { eff.DoEffect(null); count++; }

            Messages.Message($"Beacon initialized ({count} use-effect(s))", parent, MessageTypeDefOf.PositiveEvent);
        }
    }
}

