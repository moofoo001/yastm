using UnityEngine;
using Verse;

namespace YASTM
{
    public class YASTM_ModSettings : ModSettings
    {
        // --- Tweakables ---
        public float promotionObjectiveMultiplier = 1.0f;  // 0.5 .. 3.0
        public int   minDaysExtraSinceLastPromotion = 0;   // +days
        public int   tricorderCooldownSeconds = 30;        // seconds (60 ticks = 1 s)
        public int   transporterMaxRange = 15;             // tiles
        public float alertVolume = 1.0f;                   // 0.0 .. 2.0
        public int   promotionMaxConcurrent = 1;           // candidates simultaneously

        // Starfleet Aid (Comms)
        public int AidCooldownDays = 3;
        public int AidSilverCost   = 300;
        public int AidGoodwillCost = 10;
        public int AidMinGoodwill  = 20;

        // Transporter global cooldown (shared)
        public int transporterCooldownSeconds = 10;

        public override void ExposeData()
        {
            base.ExposeData();
            // Persist all values
            Scribe_Values.Look(ref promotionObjectiveMultiplier, "promotionObjectiveMultiplier", 1.0f);
            Scribe_Values.Look(ref minDaysExtraSinceLastPromotion, "minDaysExtraSinceLastPromotion", 0);
            Scribe_Values.Look(ref tricorderCooldownSeconds, "tricorderCooldownSeconds", 30);
            Scribe_Values.Look(ref transporterMaxRange, "transporterMaxRange", 15);
            Scribe_Values.Look(ref alertVolume, "alertVolume", 1.0f);
            Scribe_Values.Look(ref promotionMaxConcurrent, "promotionMaxConcurrent", 1);

            Scribe_Values.Look(ref AidCooldownDays, "AidCooldownDays", 3);
            Scribe_Values.Look(ref AidSilverCost,   "AidSilverCost",   300);
            Scribe_Values.Look(ref AidGoodwillCost, "AidGoodwillCost", 10);
            Scribe_Values.Look(ref AidMinGoodwill,  "AidMinGoodwill",  20);

            Scribe_Values.Look(ref transporterCooldownSeconds, "transporterCooldownSeconds", 10);

            ClampAll();
        }

        public void ClampAll()
        {
            promotionObjectiveMultiplier = Mathf.Clamp(promotionObjectiveMultiplier, 0.5f, 3.0f);
            minDaysExtraSinceLastPromotion = Mathf.Clamp(minDaysExtraSinceLastPromotion, 0, 30);
            tricorderCooldownSeconds = Mathf.Clamp(tricorderCooldownSeconds, 0, 600);
            transporterMaxRange = Mathf.Clamp(transporterMaxRange, 3, 100);
            alertVolume = Mathf.Clamp(alertVolume, 0f, 2f);
            promotionMaxConcurrent = Mathf.Clamp(promotionMaxConcurrent, 1, 5);

            AidCooldownDays = Mathf.Clamp(AidCooldownDays, 0, 30);
            AidSilverCost   = Mathf.Clamp(AidSilverCost,   0, 10000);
            AidGoodwillCost = Mathf.Clamp(AidGoodwillCost, 0, 100);
            AidMinGoodwill  = Mathf.Clamp(AidMinGoodwill, -100, 100);

            transporterCooldownSeconds = Mathf.Clamp(transporterCooldownSeconds, 0, 300);
        }

        // Helpers
        public int  TricorderCooldownTicks   => tricorderCooldownSeconds   * 60;
        public int  TransporterCooldownTicks => transporterCooldownSeconds * 60;
        public float AlertVolume01           => alertVolume;
    }

    public class YASTM_Mod : Mod
    {
        public static YASTM_ModSettings Settings;

        public YASTM_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<YASTM_ModSettings>();
        }

        public override string SettingsCategory() => "YASTM.Settings.Category".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var ls = new Listing_Standard();
            ls.Begin(inRect);

            // Promotion objectives multiplier
            ls.Label("YASTM.Settings.PromoMultiplier".Translate(Settings.promotionObjectiveMultiplier.ToString("0.0")));
            Settings.promotionObjectiveMultiplier = ls.Slider(Settings.promotionObjectiveMultiplier, 0.5f, 3.0f);

            ls.Gap(6);
            // Min extra days since last promotion
            ls.Label("YASTM.Settings.MinDaysSinceLastPromotion".Translate(Settings.minDaysExtraSinceLastPromotion));
            Settings.minDaysExtraSinceLastPromotion =
                Mathf.RoundToInt(ls.Slider(Settings.minDaysExtraSinceLastPromotion, 0, 30));

            ls.Gap(6);
            // Tricorder shared cooldown
            ls.Label("YASTM.Settings.TricorderCooldown".Translate(Settings.tricorderCooldownSeconds));
            Settings.tricorderCooldownSeconds =
                Mathf.RoundToInt(ls.Slider(Settings.tricorderCooldownSeconds, 0, 600));

            ls.Gap(6);
            // Transporter range
            ls.Label("YASTM.Settings.TransporterRange".Translate(Settings.transporterMaxRange));
            Settings.transporterMaxRange =
                Mathf.RoundToInt(ls.Slider(Settings.transporterMaxRange, 3, 100));

            ls.Gap(6);
            // Transporter cooldown
            ls.Label("YASTM.Settings.TransporterCooldown".Translate(Settings.transporterCooldownSeconds));
            Settings.transporterCooldownSeconds =
                Mathf.RoundToInt(ls.Slider(Settings.transporterCooldownSeconds, 0, 300));

            ls.Gap(6);
            // Alert sound volume
            ls.Label("YASTM.Settings.AlertVolume".Translate(Settings.alertVolume.ToString("0.00")));
            Settings.alertVolume = ls.Slider(Settings.alertVolume, 0f, 2f);

            ls.Gap(6);
            // Max concurrent promotion candidates
            ls.Label("YASTM.Settings.PromoMaxConcurrent".Translate(Settings.promotionMaxConcurrent));
            Settings.promotionMaxConcurrent =
                Mathf.RoundToInt(ls.Slider(Settings.promotionMaxConcurrent, 1, 5));

            ls.Gap(12);
            ls.Label("YASTM.Settings.AidCooldown".Translate(Settings.AidCooldownDays));
            Settings.AidCooldownDays = Mathf.RoundToInt(ls.Slider(Settings.AidCooldownDays, 0, 30));

            ls.Label("YASTM.Settings.AidSilver".Translate(Settings.AidSilverCost));
            Settings.AidSilverCost = Mathf.RoundToInt(ls.Slider(Settings.AidSilverCost, 0, 10000));

            ls.Label("YASTM.Settings.AidGoodwill".Translate(Settings.AidGoodwillCost));
            Settings.AidGoodwillCost = Mathf.RoundToInt(ls.Slider(Settings.AidGoodwillCost, 0, 100));

            ls.Label("YASTM.Settings.AidMinGoodwill".Translate(Settings.AidMinGoodwill));
            Settings.AidMinGoodwill = Mathf.RoundToInt(ls.Slider(Settings.AidMinGoodwill, -100, 100));

            ls.End();
        }
    }
}
