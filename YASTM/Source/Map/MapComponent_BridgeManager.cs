using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet; 
using System.Collections.Generic;
using YASTM.Source.Comps;

namespace YASTM
{
    [StaticConstructorOnStartup]
    public class MapComponent_BridgeManager : MapComponent
    {
        // Man the bridge
        public bool bridgeManningActive = false;

        // Bridge crew status
        public bool bridgeSynergyActive = false;
        private int tickCounter = 0;

        public MapComponent_BridgeManager(Map map) : base(map) { }

        public override void ExposeData()
        {
            base.ExposeData();
            // Save the switch, synergy status is calculated on the fly
            Scribe_Values.Look(ref bridgeManningActive, "bridgeManningActive", false);
        }

        //  Feature 1: The switch (called by the gizmo/button)
        public void ToggleBridgeManning()
        {
            bridgeManningActive = !bridgeManningActive;
            string status = bridgeManningActive ? "ON" : "OFF";
            Messages.Message($"Red Alert / Bridge Manning: {status}", MessageTypeDefOf.NeutralEvent);
        }

        // Feature 2: The monitoring (original logic restored)
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
            // Check every 2 seconds (120 Ticks)
            tickCounter++;
            if (tickCounter >= 120) 
            {
                CheckBridgeStatus();
                tickCounter = 0;
            }
        }

        private void CheckBridgeStatus()
        {
            bool hasCommand = false;
            bool hasHelm = false;
            bool hasSupport = false; // Ops oder Tactical

            // Scan all buildings for manned stations
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                var comp = b.GetComp<CompBridgeStation>();
                
                // Use comp.IsManned (must exist in Comp!)
                if (comp != null && comp.IsManned)
                {
                    switch (comp.Props.role)
                    {
                        case BridgeRole.Command: hasCommand = true; break;
                        case BridgeRole.Helm:    hasHelm = true; break;
                        case BridgeRole.Tactical:
                        case BridgeRole.Ops:     hasSupport = true; break;
                    }
                }
            }

            bool newState = hasCommand && hasHelm && hasSupport;

            if (newState != bridgeSynergyActive)
            {
                bridgeSynergyActive = newState;
                if (bridgeSynergyActive)
                {
                    // "ST_BridgeSynergyOnline" use Key or Fallback Text
                    Messages.Message("Bridge Synergy Online! Ship performance increased.", MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            if (Find.CurrentMap != map) return;
            if (Find.World != null && Find.World.renderer.wantedMode != WorldRenderMode.None) return;

            // Draw the icon if synergy is active
            if (bridgeSynergyActive)
            {
                float iconSize = 48f;
                // Position bottom right
                Rect rect = new Rect(Verse.UI.screenWidth - 250f, Verse.UI.screenHeight - 140f, iconSize, iconSize);

                Texture2D icon = ContentFinder<Texture2D>.Get("UI/Icons/Starfleet/Combadge", true);

                if (icon != null)
                {
                    GUI.DrawTexture(rect, icon);
                    TooltipHandler.TipRegion(rect, "Bridge Synergy Active: Crew is operating at peak efficiency.");
                }
            }
        }
    }
}