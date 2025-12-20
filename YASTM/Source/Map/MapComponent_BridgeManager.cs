using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet; 
using System.Collections.Generic;
using YASTM.Source.Comps;

namespace YASTM.Source.Map
{
    [StaticConstructorOnStartup]
    public class MapComponent_BridgeManager : MapComponent
    {
        private int tickCounter = 0;
        public bool bridgeSynergyActive = false;

        public MapComponent_BridgeManager(Verse.Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
            // Check alle 2 Sekunden (120 Ticks)
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
            bool hasSupport = false; 

            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                var comp = b.GetComp<CompBridgeStation>();
                if (comp != null && comp.IsManned)
                {
                    switch (comp.Props.role)
                    {
                        case BridgeRole.Command: hasCommand = true; break;
                        case BridgeRole.Helm:    hasHelm = true; break;
                        case BridgeRole.Tactical:
                        case BridgeRole.Science:
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
                    Messages.Message("ST_BridgeSynergyOnline".Translate(), MessageTypeDefOf.PositiveEvent);
                }
                // Optional: Meldung bei Verlust
                // else { Messages.Message("ST_BridgeSynergyLost".Translate(), MessageTypeDefOf.NegativeEvent); }
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            if (Find.CurrentMap != map) return;
            if (Find.World != null && Find.World.renderer.wantedMode != WorldRenderMode.None) return;

            // Nur zeichnen, wenn aktiv
            if (bridgeSynergyActive)
            {
                // UI Positionierung (Unten rechts)
                float iconSize = 48f;
                Rect rect = new Rect(Verse.UI.screenWidth - 250f, Verse.UI.screenHeight - 140f, iconSize, iconSize);

                Texture2D icon = ContentFinder<Texture2D>.Get("UI/Icons/Starfleet/Combadge", true);

                if (icon != null)
                {
                    GUI.DrawTexture(rect, icon);
                    TooltipHandler.TipRegion(rect, "ST_BridgeSynergyActiveDesc".Translate());
                }
            }
        }
    }
}