using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    [HarmonyPatch(typeof(MapInterface), "MapInterfaceOnGUI_AfterMainTabs")]
    public static class Patch_StardateOverlay
    {
        public static void Postfix()
        {
            // Check if the mod is enabled
            if (!YASTM_Mod.Settings.showStardateOverlay) return;

            // Check if the game is in screenshot mode or if there is no map
            if (Find.CurrentMap == null || Find.UIRoot.screenshotMode.FiltersCurrentEvent) return;
            
            float baseStardate = 1739.12f;
            float stardate = baseStardate + GenDate.DaysPassedFloat;
            string text = $"STARDATE {stardate:F1} | THE FEDERATION OF PLANETS";

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            Vector2 size = Text.CalcSize(text);
            size.x += 30f;
            size.y += 8f;

            float rightMargin = 150f;
            float bottomMargin = 80f; 

            Rect rect = new Rect(Verse.UI.screenWidth - size.x - rightMargin, Verse.UI.screenHeight - bottomMargin, size.x, size.y);

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            
            GUI.color = new Color(1f, 0.6f, 0f, 1f); 
            Widgets.DrawBox(rect, 2); 

            GUI.color = new Color(0.6f, 0.8f, 1f, 1f);
            Rect textRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            Widgets.Label(textRect, text);

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
    }
}