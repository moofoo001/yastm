using System.Collections.Generic;
using System.Linq; 
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
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;

            if (parent.Faction == Faction.OfPlayer)
            {
                // BUTTON 1: SHUTTLE BAUEN
                Command_Action buildShuttle = new Command_Action();
                buildShuttle.defaultLabel = "ST_CallOwnShuttle".Translate();
                buildShuttle.defaultDesc = "ST_CallOwnShuttleDesc".Translate();
                
                if (!Props.buildIconPath.NullOrEmpty()) 
                    buildShuttle.icon = ContentFinder<Texture2D>.Get(Props.buildIconPath);
                else 
                    buildShuttle.icon = ContentFinder<Texture2D>.Get("UI/Designators/Build"); 
                
                buildShuttle.action = delegate { PlaceShuttleBlueprint(); };
                
                // Wir deaktivieren den Button NICHT, damit wir sehen, ob er überhaupt klickt
                // if (IsShuttlePresent() || IsBlueprintPresent()) buildShuttle.Disable("ST_ShuttleBayOccupied".Translate());
                
                yield return buildShuttle;

                // BUTTON 2: MIETEN
                Command_Action rentShuttle = new Command_Action();
                rentShuttle.defaultLabel = "ST_RentShuttle".Translate(RentalCost); 
                rentShuttle.defaultDesc = "ST_RentShuttleDesc".Translate(RentalCost);
                
                if (!Props.rentIconPath.NullOrEmpty())
                    rentShuttle.icon = ContentFinder<Texture2D>.Get(Props.rentIconPath);
                else
                    rentShuttle.icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CallAid");

                // Einfacher Check
                bool canAfford = (parent.Map.resourceCounter.GetCount(ThingDefOf.Silver) >= RentalCost);
                
                if (!canAfford) rentShuttle.Disable("NotEnoughStoredLower".Translate());
                
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

        // --- DEBUG VERSION DES BLUEPRINT PLACEMENT ---
        private void PlaceShuttleBlueprint()
        {
            Log.Message("[YASTM] Debug: Button clicked.");

            if (Props.shuttleDef == null)
            {
                Log.Error("[YASTM] Critical Error: Props.shuttleDef is NULL! Check your XML <shuttleDef> tag.");
                return;
            }

            Log.Message($"[YASTM] Debug: ShuttleDef found: {Props.shuttleDef.defName}");
            
            if (Props.shuttleDef.blueprintDef == null)
            {
                Log.Error("[YASTM] Critical Error: The Shuttle has no BlueprintDef! (Did you remove <designationCategory>? Put it back!)");
                return;
            }

            IntVec3 buildLoc = CalculateCenterPosition(Props.shuttleDef);
            Log.Message($"[YASTM] Debug: Calculated Center: {buildLoc}");

            // VERSUCH 1: Standard
            Blueprint bp = GenConstruct.PlaceBlueprintForBuild(Props.shuttleDef, buildLoc, parent.Map, Rot4.North, Faction.OfPlayer, null);
            
            if (bp != null)
            {
                Log.Message("[YASTM] Success: Standard placement worked.");
            }
            else
            {
                Log.Warning("[YASTM] Warning: Standard placement failed (Blocked?). Trying FORCE SPAWN.");
                
                // VERSUCH 2: Gewalt
                try
                {
                    Thing spawnedBlueprint = GenSpawn.Spawn(Props.shuttleDef.blueprintDef, buildLoc, parent.Map, Rot4.North);
                    spawnedBlueprint.SetFaction(Faction.OfPlayer);
                    Log.Message("[YASTM] Success: Force Spawn worked!");
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[YASTM] Error: Force spawn failed too. Reason: {ex.Message}");
                }
            }
        }

        private IntVec3 CalculateCenterPosition(ThingDef shuttleDef)
        {
            int xOffset = (parent.def.size.x - shuttleDef.size.x) / 2 - 1; 
            if (xOffset < 0) xOffset = 0;
            int zOffset = Mathf.CeilToInt((parent.def.size.z - shuttleDef.size.z) / 2f);
            return parent.Position + new IntVec3(xOffset, 0, zOffset);
        }

        private void PaySilverManual(int amount)
        {
            int remaining = amount;
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

        private void SpawnRentalShuttle(ThingDef defToSpawn)
        {
            if (defToSpawn == null) return;
            IntVec3 spawnLoc = CalculateCenterPosition(defToSpawn);
            Thing shuttle = GenSpawn.Spawn(defToSpawn, spawnLoc, parent.Map);
            shuttle.SetFaction(Faction.OfPlayer);
            CompRefuelable refuelable = shuttle.TryGetComp<CompRefuelable>();
            if (refuelable != null) refuelable.Refuel(refuelable.Props.fuelCapacity);
            Messages.Message("ST_ShuttleArrived".Translate(), shuttle, MessageTypeDefOf.PositiveEvent);
        }

        // Helper für Gizmo-Status (habe ich im Button oben kurz deaktiviert für Debug)
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
            foreach (IntVec3 cell in rect) {
                List<Thing> things = parent.Map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < things.Count; i++) {
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
        public string buildIconPath;
        public string rentIconPath;

        public CompProperties_ShuttleBay()
        {
            this.compClass = typeof(CompShuttleBay);
        }
    }
}