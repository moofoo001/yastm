using UnityEngine;
using Verse;
using System;

namespace YASTM
{
    public class YASTM_Mod : Mod
    {
        public static YASTM_ModSettings Settings;

        private enum Tab
        {
            General,
            Incidents,
            Debug
        }
        private Tab currentTab = Tab.General;

        public YASTM_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<YASTM_ModSettings>();
        }

        public override string SettingsCategory()
        {
            return "YASTM (Star Trek)";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect tabRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Rect contentRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);

            float tabWidth = inRect.width / 3f;
            if (Widgets.ButtonText(new Rect(tabRect.x, tabRect.y, tabWidth, tabRect.height), "General & UI", true, true, currentTab != Tab.General))
                currentTab = Tab.General;
            if (Widgets.ButtonText(new Rect(tabRect.x + tabWidth, tabRect.y, tabWidth, tabRect.height), "Incidents & Events", true, true, currentTab != Tab.Incidents))
                currentTab = Tab.Incidents;
            if (Widgets.ButtonText(new Rect(tabRect.x + tabWidth * 2, tabRect.y, tabWidth, tabRect.height), "Debug", true, true, currentTab != Tab.Debug))
                currentTab = Tab.Debug;

            Listing_Standard ls = new Listing_Standard();
            ls.Begin(contentRect);

            switch (currentTab)
            {
                case Tab.General:
                    DrawGeneralTab(ls);
                    break;
                case Tab.Incidents:
                    DrawIncidentsTab(ls);
                    break;
                case Tab.Debug:
                    DrawDebugTab(ls);
                    break;
            }

            ls.End();
            base.DoSettingsWindowContents(inRect);
        }

        private void DrawGeneralTab(Listing_Standard ls)
        {
            ls.Label("UI Settings".Colorize(Color.cyan));
            ls.CheckboxLabeled("Show Stardate Overlay", ref Settings.showStardateOverlay, "Toggles the Star Trek Stardate display on the main screen.");
            
            ls.CheckboxLabeled("Show Memory Alpha Tab", ref Settings.showMemoryAlphaTab, "Toggles the visibility of the Memory Alpha database button in the bottom menu bar.");
            
            ls.GapLine();

            ls.Label("Gameplay Mechanics".Colorize(Color.cyan));
            ls.CheckboxLabeled(
                "Enable Faction Research Lock", 
                ref Settings.enableFactionResearchLock, 
                "If enabled, specific technologies (like Klingon engineering) can only be researched by their respective factions. Disable to unlock the full tech tree for everyone."
            );
            
            ls.GapLine();
            ls.Label("Career & Promotion".Colorize(Color.cyan));
            ls.Label($"Promotion Objective Multiplier: {Settings.promotionObjectiveMultiplier:F1}x");
            Settings.promotionObjectiveMultiplier = ls.Slider(Settings.promotionObjectiveMultiplier, 0.1f, 5.0f);
            
            ls.Label($"Min Days Between Promotions: {Settings.minDaysBetweenPromotions}");
            Settings.minDaysBetweenPromotions = (int)ls.Slider(Settings.minDaysBetweenPromotions, 1f, 60f);

            ls.GapLine();
            
            ls.Label("Starfleet Aid".Colorize(Color.cyan));
            ls.CheckboxLabeled("Enable Starfleet Aid Drops", ref Settings.enableStarfleetAid);
            if (Settings.enableStarfleetAid)
            {
                ls.Label($"Aid Cooldown (Days): {Settings.AidCooldownDays}");
                Settings.AidCooldownDays = (int)ls.Slider(Settings.AidCooldownDays, 1f, 60f);
            }
        }

        private void DrawIncidentsTab(Listing_Standard ls)
        {
            ls.Label("Event Toggles".Colorize(Color.cyan));
            ls.CheckboxLabeled("Enable Q Visits", ref Settings.enableQVisits, "Allows the Q Continuum to randomly interfere with your colony.");
            ls.CheckboxLabeled("Enable Badgey Sabotage", ref Settings.enableBadgeyIncidents, "Badgey might occasionally take over your consoles.");
            ls.CheckboxLabeled("Enable Holodeck Failures", ref Settings.enableHolodeckFailures, "Safety protocols can go offline, causing hostile holograms.");
            
            ls.GapLine();

            ls.Label("Threat Scaling".Colorize(Color.cyan));
            ls.CheckboxLabeled("Tribble Multiplication", ref Settings.enableTribbleMultiplication, "If disabled, Tribbles will stop multiplying automatically.");
            if (Settings.enableTribbleMultiplication)
            {
                ls.Label($"Tribble Spawn Rate Multiplier: {Settings.tribbleSpawnRateMultiplier:F1}x");
                Settings.tribbleSpawnRateMultiplier = ls.Slider(Settings.tribbleSpawnRateMultiplier, 0.1f, 5.0f);
            }

            ls.Gap();
            ls.Label($"Warp Core Breach Chance Multiplier: {Settings.warpCoreBreachChanceMultiplier:F1}x");
            Settings.warpCoreBreachChanceMultiplier = ls.Slider(Settings.warpCoreBreachChanceMultiplier, 0.0f, 5.0f);
        }

        private void DrawDebugTab(Listing_Standard ls)
        {
            ls.Label("Developer Tools".Colorize(Color.cyan));
            ls.CheckboxLabeled("Enable YASTM Debug Mode", ref Settings.debugMode, "Prints additional logs to the console for troubleshooting.");
            
            if (Settings.debugMode)
            {
                ls.Label("Debug mode is ACTIVE. Console spam may occur.", tooltip: "Disable for normal gameplay.");
            }
        }
    }
}