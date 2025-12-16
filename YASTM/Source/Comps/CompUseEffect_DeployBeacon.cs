using RimWorld;
using Verse;
using Verse.Sound;

namespace StarTrekFactions.Comps
{
    public class CompProperties_UseEffect_DeployBeacon : CompProperties_UseEffect
    {
        public ThingDef buildingDef;      
        public bool placeNearUser = true; 

        public CompProperties_UseEffect_DeployBeacon()
        {
            compClass = typeof(CompUseEffect_DeployBeacon);
        }
    }

    public class CompUseEffect_DeployBeacon : CompUseEffect
    {
        public CompProperties_UseEffect_DeployBeacon Props
            => (CompProperties_UseEffect_DeployBeacon)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (Props.buildingDef == null) return;

            Map map = parent.MapHeld ?? usedBy.MapHeld;
            if (map == null) return;

            var thing = ThingMaker.MakeThing(Props.buildingDef);
            var cell  = parent.PositionHeld.IsValid ? parent.PositionHeld : usedBy.PositionHeld;

            bool placed;
            if (Props.placeNearUser)
            {
                placed = GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }
            else
            {
                GenSpawn.Spawn(thing, cell, map);
                placed = true;
            }

            if (placed)
            {
                
                thing.SetFaction(Faction.OfPlayer);
                thing.SetForbidden(false, false);

                
                parent.Destroy(DestroyMode.Vanish);

                SoundStarter.PlayOneShot(SoundDefOf.Click, SoundInfo.OnCamera());
                Messages.Message("ST.DeployedBeacon".Translate(), thing, MessageTypeDefOf.PositiveEvent, false);
            }
            else
            {
                Messages.Message("ST.NoPlaceToDeploy".Translate(), parent, MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}

