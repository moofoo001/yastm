using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet; // Wichtig für WorldRenderMode
using System.Collections.Generic;
using YASTM.Source.Comps;

namespace YASTM.Source.Map
{
    public class MapComponent_BridgeManager : MapComponent
    {
        private int tickCounter = 0;
        public bool bridgeSynergyActive = false;
        
        // UI Texturen
        private static readonly Texture2D IconSynergyOn = ContentFinder<Texture2D>.Get("UI/Icons/Starfleet/Combadge", true);
        
        // Cache aller Stationen auf der Map
        private List<CompBridgeStation> cachedStations = new List<CompBridgeStation>();

        public MapComponent_BridgeManager(Verse.Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
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
            bool hasOps = false;
            bool hasTactical = false;

            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                var comp = b.GetComp<CompBridgeStation>();
                if (comp != null && comp.IsManned) 
                {
                    if (comp.Props.role == BridgeRole.Command) hasCommand = true;
                    if (comp.Props.role == BridgeRole.Ops) hasOps = true;
                    if (comp.Props.role == BridgeRole.Tactical) hasTactical = true;
                }
            }

            bool newState = hasCommand && (hasOps || hasTactical);

            if (newState != bridgeSynergyActive)
            {
                bridgeSynergyActive = newState;
                if (bridgeSynergyActive)
                {
                    Messages.Message("ST_BridgeSynergyOnline".Translate(), MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        // Das "Fancy UI" Overlay
        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            // 1. Prüfen, ob wir auf der richtigen Karte sind
            if (Find.CurrentMap != map) return;

            // 2. FIX: Prüfen, ob die Weltkarte offen ist (ohne WorldRendererUtility)
            // Wenn der Renderer-Modus NICHT "None" ist, sehen wir gerade den Planeten -> Abbruch
            if (Find.World != null && Find.World.renderer.wantedMode != WorldRenderMode.None) return;

            float iconSize = 48f;

            // UI Position
            Rect rect = new Rect(Verse.UI.screenWidth - 250f, Verse.UI.screenHeight - 140f, iconSize, iconSize);

            // Icon Zeichnen
            if (bridgeSynergyActive)
            {
                if (IconSynergyOn != null)
                    GUI.DrawTexture(rect, IconSynergyOn);
                
                TooltipHandler.TipRegion(rect, "ST_BridgeSynergyActiveDesc".Translate());
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);
                
                if (IconSynergyOn != null) 
                    GUI.DrawTexture(rect, IconSynergyOn); 
                
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, "ST_BridgeSynergyOfflineDesc".Translate());
            }
        }
    }
}