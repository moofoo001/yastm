// YASTM Patch_StardateOverlay v1.1
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    [StaticConstructorOnStartup]
    public static class YASTM_StardateInit
    {
        static YASTM_StardateInit()
        {
            //Log.Message("[YASTM DEBUG] Initialized Patch_StardateOverlay v1.1");
        }
    }

    [HarmonyPatch(typeof(MapInterface), "MapInterfaceOnGUI_AfterMainTabs")]
    public static class Patch_StardateOverlay
    {
        public static void Postfix()
        {
            if (!YASTM_Mod.Settings.showStardateOverlay) return;
            if (Find.CurrentMap == null || Find.UIRoot.screenshotMode.FiltersCurrentEvent) return;
            
            float baseStardate = 1739.12f;
            float stardate = baseStardate + GenDate.DaysPassedFloat;

            // gets faction name
            string factionName = "UNKNOWN FACTION";
            if (Faction.OfPlayer != null)
            {
                factionName = Faction.OfPlayer.HasName ? Faction.OfPlayer.Name.ToUpper() : Faction.OfPlayer.def.LabelCap.Resolve().ToUpper();
            }

            string text = $"STARDATE {stardate:F1} | {factionName}";

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            Vector2 size = Text.CalcSize(text);
            size.x += 30f;
            size.y += 8f;

            float rightMargin = 150f; // right margin
            float bottomMargin = 160f; // bottom margin 

            Rect rect = new Rect(Verse.UI.screenWidth - size.x - rightMargin, Verse.UI.screenHeight - bottomMargin, size.x, size.y);

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            
            GUI.color = new Color(1f, 0.6f, 0f, 1f); 
            Widgets.DrawBox(rect, 2); 

            GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f); 
            Widgets.Label(rect, text);

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
    }
}