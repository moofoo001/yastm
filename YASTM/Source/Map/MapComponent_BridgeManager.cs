using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet; 
using System.Collections.Generic;
using YASTM.Source.Comps;

namespace YASTM.Source.Map
{
    public class MapComponent_BridgeManager : MapComponent
    {
        private int tickCounter = 0;
        public bool bridgeSynergyActive = false;
        
        // FIX: Unbenutztes Feld 'iconSynergyOff' entfernt
        private static Texture2D iconSynergyOn;

        // Lazy Loading Property
        public static Texture2D IconSynergyOn
        {
            get
            {
                if (iconSynergyOn == null)
                    iconSynergyOn = ContentFinder<Texture2D>.Get("UI/Icons/Starfleet/Combadge", true);
                return iconSynergyOn;
            }
        }

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

            // Suche optimieren: Nur Gebäude des Spielers
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

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            if (Find.CurrentMap != map) return;
            // Prüfen ob Weltkarte offen ist
            if (Find.World != null && Find.World.renderer.wantedMode != WorldRenderMode.None) return;

            float iconSize = 48f;
            Rect rect = new Rect(Verse.UI.screenWidth - 250f, Verse.UI.screenHeight - 140f, iconSize, iconSize);

            if (bridgeSynergyActive)
            {
                if (IconSynergyOn != null)
                    GUI.DrawTexture(rect, IconSynergyOn);
                
                TooltipHandler.TipRegion(rect, "ST_BridgeSynergyActiveDesc".Translate());
            }
            else
            {
                // Wir nutzen das gleiche Icon, aber transparent
                GUI.color = new Color(1f, 1f, 1f, 0.3f);
                if (IconSynergyOn != null) 
                    GUI.DrawTexture(rect, IconSynergyOn); 
                GUI.color = Color.white;
                
                TooltipHandler.TipRegion(rect, "ST_BridgeSynergyOfflineDesc".Translate());
            }
        }
    }
}