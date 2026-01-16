using System.Collections.Generic;
using System.Linq; // WICHTIG für .ToList()
using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompShuttleBay : ThingComp
    {
        public CompProperties_ShuttleBay Props => (CompProperties_ShuttleBay)props;
        
        private const int RentalCost = 500; 
        private const string RentalShuttleDefName = "ST_Shuttle_Taxi"; 

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (parent.Faction == Faction.OfPlayer)
            {
                // build shuttle
                Command_Action buildShuttle = new Command_Action();
                buildShuttle.defaultLabel = "ST_CallOwnShuttle".Translate();
                buildShuttle.defaultDesc = "ST_CallOwnShuttleDesc".Translate();
                
                if (!Props.iconPath.NullOrEmpty()) buildShuttle.icon = ContentFinder<Texture2D>.Get(Props.iconPath);
                
                buildShuttle.action = delegate 
                { 
                    PlaceShuttleBlueprint(); 
                };
                
                if (IsShuttlePresent() || IsBlueprintPresent()) 
                {
                    buildShuttle.Disable("ST_ShuttleBayOccupied".Translate());
                }
                
                yield return buildShuttle;


                // rental
                Command_Action rentShuttle = new Command_Action();
                rentShuttle.defaultLabel = "ST_RentShuttle".Translate(RentalCost); 
                rentShuttle.defaultDesc = "ST_RentShuttleDesc".Translate(RentalCost);
                rentShuttle.icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CallAid"); 

                bool canAfford = GetTotalSilverInColony() >= RentalCost;

                if (!canAfford)
                {
                    rentShuttle.Disable("NotEnoughStoredLower".Translate());
                }
                else if (IsShuttlePresent() || IsBlueprintPresent())
                {
                    rentShuttle.Disable("ST_ShuttleBayOccupied".Translate());
                }

                rentShuttle.action = delegate
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "ST_ConfirmPayment".Translate(RentalCost),
                        delegate
                        {
                            PaySilverManual(RentalCost);
                            ThingDef taxiDef = DefDatabase<ThingDef>.GetNamed(RentalShuttleDefName);
                            SpawnRentalShuttle(taxiDef);
                        }
                    ));
                };

                yield return rentShuttle;
            }
        }

        private int GetTotalSilverInColony()
        {
            return parent.Map.resourceCounter.GetCount(ThingDefOf.Silver);
        }

        private void PaySilverManual(int amount)
        {
            int remaining = amount;
            

            // save list / store backup list
            List<Thing> silverStacks = parent.Map.listerThings.ThingsOfDef(ThingDefOf.Silver).ToList();
            
            foreach (Thing silver in silverStacks)
            {
                if (remaining <= 0) break;
                if (silver.Destroyed) continue;

                int toTake = Mathf.Min(silver.stackCount, remaining);
                silver.SplitOff(toTake).Destroy();
                remaining -= toTake;
            }
        }

        private IntVec3 CalculateCenterPosition(ThingDef shuttleDef)
        {

            // shuttle placment fix
            
            int xOffset = (parent.def.size.x - shuttleDef.size.x) / 2 - 1;
            if (xOffset < 0) xOffset = 0; 
            int zOffset = Mathf.CeilToInt((parent.def.size.z - shuttleDef.size.z) / 2f); 
            return parent.Position + new IntVec3(xOffset, 0, zOffset);
        }

        private void PlaceShuttleBlueprint()
        {
            if (Props.shuttleDef == null) return;

            IntVec3 buildLoc = CalculateCenterPosition(Props.shuttleDef);
            GenConstruct.PlaceBlueprintForBuild(Props.shuttleDef, buildLoc, parent.Map, Rot4.North, Faction.OfPlayer, null);
        }

        private void SpawnRentalShuttle(ThingDef defToSpawn)
        {
            if (defToSpawn == null) return;

            IntVec3 spawnLoc = CalculateCenterPosition(defToSpawn);
            Thing shuttle = GenSpawn.Spawn(defToSpawn, spawnLoc, parent.Map);
            shuttle.SetFaction(Faction.OfPlayer);

            CompRefuelable refuelable = shuttle.TryGetComp<CompRefuelable>();
            if (refuelable != null)
            {
                refuelable.Refuel(refuelable.Props.fuelCapacity);
            }
            
            Messages.Message("ST_ShuttleArrived".Translate(), shuttle, MessageTypeDefOf.PositiveEvent);
        }

        private bool IsShuttlePresent()
        {
            foreach (Thing t in GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, 4f, true))
            {
                if (t.def == Props.shuttleDef || t.def.defName == RentalShuttleDefName) return true;
            }
            return false;
        }

        private bool IsBlueprintPresent()
        {
            CellRect rect = parent.OccupiedRect();
            foreach (IntVec3 cell in rect)
            {
                List<Thing> things = parent.Map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Blueprint bp && bp.def.entityDefToBuild == Props.shuttleDef) return true;
                    if (things[i] is Frame frame && frame.def.entityDefToBuild == Props.shuttleDef) return true;
                }
            }
            return false;
        }
    }

    public class CompProperties_ShuttleBay : CompProperties
    {
        public ThingDef shuttleDef;
        public string iconPath;

        public CompProperties_ShuttleBay()
        {
            this.compClass = typeof(CompShuttleBay);
        }
    }
}