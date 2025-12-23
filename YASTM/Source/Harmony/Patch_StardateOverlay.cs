using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    public static class Patch_StardateOverlay
    {
        // colors
        private static readonly Color LcarsColor = new Color(1.0f, 0.6f, 0.0f, 1.0f); 
        private static readonly Color LcarsTextColor = new Color(0.1f, 0.1f, 0.1f, 1f); 

        // 
        private static Texture2D _lcarsCapTex;

        public static void Postfix()
        {
            if (Find.CurrentMap == null || Find.World == null) return;
            if (Find.UIRoot.screenshotMode.FiltersCurrentEvent) return;

            //
            if (_lcarsCapTex == null)
            {
                _lcarsCapTex = GenerateRightCapTexture();
            }

            DrawLcarsHeader();
        }

        private static void DrawLcarsHeader()
        {
            // 
            float baseStardate = 1739.12f;
            float currentStardate = baseStardate + GenDate.DaysPassedFloat; 

            // 
            string factionName = Faction.OfPlayer.Name.ToUpper();
            string text = $"STARDATE {currentStardate:F1} | {factionName}";

            // 
            Text.Font = GameFont.Small; 
            Vector2 textSize = Text.CalcSize(text);
            
            float barHeight = 20f;
            float barWidth = textSize.x + 20f; 
            
            // 
            float capWidth = barHeight / 2f; 

            float screenW = Verse.UI.screenWidth;

            float xPos = screenW - barWidth - capWidth - 280f; 
            float yPos = 10f;

            Rect barRect = new Rect(xPos, yPos, barWidth, barHeight);

            // 4.
            
            // A) 
            Widgets.DrawRectFast(barRect, LcarsColor);

            // B)
            Rect capRect = new Rect(barRect.xMax + 2f, yPos, capWidth, barHeight);
            
            Color oldColor = GUI.color;
            GUI.color = LcarsColor;
            GUI.DrawTexture(capRect, _lcarsCapTex);

            // C)
            GUI.color = LcarsTextColor;
            Rect textRect = new Rect(barRect.x, barRect.y, barRect.width - 5f, barRect.height);
            Text.Anchor = TextAnchor.MiddleRight; 
            Widgets.Label(textRect, text);

            // 5. CLEANUP
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = oldColor;
            Text.Font = GameFont.Small;
        }


        private static Texture2D GenerateRightCapTexture()
        {

            int height = 64;
            int width = 32; // 
            
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            

            Vector2 center = new Vector2(0f, height / 2f - 0.5f);
            float radius = height / 2f - 1f; 

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {

                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}