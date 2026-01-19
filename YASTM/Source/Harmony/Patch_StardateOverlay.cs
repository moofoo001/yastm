using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    [HarmonyPatch(typeof(GlobalControlsUtility), "DoDate")]
    public static class Patch_StardateOverlay
    {
        private static Texture2D cachedBackground;
        
        
        
        private static readonly Vector2 BGSize = new Vector2(490f, 28f); 
        
        private const float TopMargin = 12f; 
        

        // negative left, positive right
        private const float CenterOffset = 360f; 
        
        // ---------------------

        public static void Prefix(ref float leftX, float width)
        {
            if (cachedBackground == null)
            {
                DetermineFactionBackground();
            }


            // center 
            float xPos = (Verse.UI.screenWidth - BGSize.x) / 2f;
            
            // place
            xPos += CenterOffset;
            
            Rect overlayRect = new Rect(xPos, TopMargin, BGSize.x, BGSize.y);
            
            // background
            GUI.color = Color.white; 
            GUI.DrawTexture(overlayRect, cachedBackground);

            // data
            float baseStardate = 1739.12f;
            float currentStardate = baseStardate + GenDate.DaysPassedFloat; 
            
            string factionName = "UNKNOWN";
            if (Faction.OfPlayer != null && Faction.OfPlayer.Name != null)
            {
                factionName = Faction.OfPlayer.Name.ToUpper();
            }

            string text = $"STARDATE {currentStardate:F1} | {factionName}";

            // style
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleRight; 
            Text.Font = GameFont.Small;           
            GUI.color = GetReadableTextColor(); 

            // text positioning
            Rect textRect = new Rect(
                overlayRect.x + 50f,        // logo
                overlayRect.y, 
                overlayRect.width - 90f,    // right 
                overlayRect.height - 2f
            );
            
            bool oldWrap = Text.WordWrap;
            Text.WordWrap = false; 
            
            Widgets.Label(textRect, text);

            // cleanup
            Text.WordWrap = oldWrap;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }

        private static Color GetReadableTextColor()
        {
            if (Faction.OfPlayer == null || Faction.OfPlayer.def == null) return new Color(0.1f, 0.1f, 0.1f);

            string defName = Faction.OfPlayer.def.defName;

            if (defName.Contains("Klingon") || defName.Contains("Romulan")) 
                return new Color(0.9f, 0.9f, 0.9f); 
            
            return new Color(0.1f, 0.1f, 0.1f); 
        }

        private static void DetermineFactionBackground()
        {
            if (Faction.OfPlayer == null || Faction.OfPlayer.def == null)
            {
                cachedBackground = ContentFinder<Texture2D>.Get("UI/Overlays/Stardate_Overlay"); 
                return;
            }

            string playerDef = Faction.OfPlayer.def.defName;

            if (playerDef.Contains("Klingon"))
                cachedBackground = ContentFinder<Texture2D>.Get("UI/Overlays/Stardate_Klingon", false);
            else if (playerDef.Contains("Romulan"))
                cachedBackground = ContentFinder<Texture2D>.Get("UI/Overlays/Stardate_Romulan", false);
            else if (playerDef.Contains("Ferengi"))
                cachedBackground = ContentFinder<Texture2D>.Get("UI/Overlays/Stardate_Ferengi", false);
            else
                cachedBackground = ContentFinder<Texture2D>.Get("UI/Overlays/Stardate_Overlay"); 

            if (cachedBackground == null) 
                cachedBackground = ContentFinder<Texture2D>.Get("UI/Overlays/Stardate_Overlay");
        }
        
        public static void ResetCache()
        {
            cachedBackground = null;
        }
        [HarmonyPatch(typeof(Game), "FinalizeInit")] 
        public static class Patch_ClearOverlayCache
        {
            public static void Postfix()
            {
                // clear cache
                Patch_StardateOverlay.ResetCache();
                Log.Message("[YASTM] Stardate Overlay Cache cleared for new game.");
            }
        }
    }
}