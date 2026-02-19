using UnityEngine;
using Verse;

namespace YASTM
{
    public class YASTM_ModSettings : ModSettings
    {
        // --- UI & Overlays ---
        public bool showStardateOverlay = true;

        // --- Gameplay / Careers ---
        public float promotionObjectiveMultiplier = 1.0f;
        public int minDaysBetweenPromotions = 5;

        // --- Incidents & Events ---
        public bool enableTribbleMultiplication = true;
        public float tribbleSpawnRateMultiplier = 1.0f;
        public bool enableQVisits = true;
        public bool enableBadgeyIncidents = true;
        public bool enableHolodeckFailures = true;
        public float warpCoreBreachChanceMultiplier = 1.0f;

        // --- Transporter & Gear ---
        public int transporterMaxRange = 999;
        public int TransporterCooldownTicks = 2500;
        public int TricorderCooldownTicks = 1250;

        // --- Force Fields ---
        public int forceFieldRadius = 6;
        public int forceFieldRefreshTicks = 60;
        public bool forceFieldHostilesOnly = true;

        // --- Starfleet Aid ---
        public bool enableStarfleetAid = true;
        public int AidCooldownDays = 15;
        public int AidSilverCost = 500;
        public int AidGoodwillCost = 10;
        public int AidMinGoodwill = 20;

        // --- Debug ---
        public bool debugMode = false;

        public override void ExposeData()
        {
            base.ExposeData();
            
            // UI
            Scribe_Values.Look(ref showStardateOverlay, "showStardateOverlay", true);
            
            // Career
            Scribe_Values.Look(ref promotionObjectiveMultiplier, "promotionObjectiveMultiplier", 1.0f);
            Scribe_Values.Look(ref minDaysBetweenPromotions, "minDaysBetweenPromotions", 5);
            
            // Incidents
            Scribe_Values.Look(ref enableTribbleMultiplication, "enableTribbleMultiplication", true);
            Scribe_Values.Look(ref tribbleSpawnRateMultiplier, "tribbleSpawnRateMultiplier", 1.0f);
            Scribe_Values.Look(ref enableQVisits, "enableQVisits", true);
            Scribe_Values.Look(ref enableBadgeyIncidents, "enableBadgeyIncidents", true);
            Scribe_Values.Look(ref enableHolodeckFailures, "enableHolodeckFailures", true);
            Scribe_Values.Look(ref warpCoreBreachChanceMultiplier, "warpCoreBreachChanceMultiplier", 1.0f);
            
            // Gear & Transporter
            Scribe_Values.Look(ref transporterMaxRange, "transporterMaxRange", 999);
            Scribe_Values.Look(ref TransporterCooldownTicks, "TransporterCooldownTicks", 2500);
            Scribe_Values.Look(ref TricorderCooldownTicks, "TricorderCooldownTicks", 1250);

            // Force Fields
            Scribe_Values.Look(ref forceFieldRadius, "forceFieldRadius", 6);
            Scribe_Values.Look(ref forceFieldRefreshTicks, "forceFieldRefreshTicks", 60);
            Scribe_Values.Look(ref forceFieldHostilesOnly, "forceFieldHostilesOnly", true);

            // Aid
            Scribe_Values.Look(ref enableStarfleetAid, "enableStarfleetAid", true);
            Scribe_Values.Look(ref AidCooldownDays, "AidCooldownDays", 15);
            Scribe_Values.Look(ref AidSilverCost, "AidSilverCost", 500);
            Scribe_Values.Look(ref AidGoodwillCost, "AidGoodwillCost", 10);
            Scribe_Values.Look(ref AidMinGoodwill, "AidMinGoodwill", 20);
            
            // Debug
            Scribe_Values.Look(ref debugMode, "debugMode", false);
        }
    }
}