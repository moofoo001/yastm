using UnityEngine;
using Verse;

namespace YASTM
{
    public class YASTM_ModSettings : ModSettings
    {
        // --- Tweakables ---
        public float promotionObjectiveMultiplier = 1.0f;  // 0.5 .. 3.0
        public int   minDaysExtraSinceLastPromotion = 0;   // +Tage obendrauf
        public int   tricorderCooldownSeconds = 30;        // Sek. (60 Ticks = 1 Sek.)
        public int   transporterMaxRange = 15;             // Tiles
        public float alertVolume = 1.0f;                   // 0.0 .. 2.0
        public int   promotionMaxConcurrent = 1;           // concurrent promotion canidate
        public int AidCooldownDays = 3;   // 0..30
        public int AidSilverCost   = 300; // 0..10000
        public int AidGoodwillCost = 10;  // 0..100
        public int AidMinGoodwill  = 20;  // -100..100
        public int transporterCooldownSeconds = 10;    // 0..300

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref AidCooldownDays, "AidCooldownDays", 3);
            Scribe_Values.Look(ref AidSilverCost,   "AidSilverCost",   300);
            Scribe_Values.Look(ref AidGoodwillCost, "AidGoodwillCost", 10);
            Scribe_Values.Look(ref AidMinGoodwill, "AidMinGoodwill", 20);
            Scribe_Values.Look(ref transporterCooldownSeconds, "transporterCooldownSeconds", 10);
            ClampAll();
        }

        public void ClampAll()
        {
            AidCooldownDays = Mathf.Clamp(AidCooldownDays, 0, 30);
            AidSilverCost   = Mathf.Clamp(AidSilverCost,   0, 10000);
            AidGoodwillCost = Mathf.Clamp(AidGoodwillCost, 0, 100);
            AidMinGoodwill = Mathf.Clamp(AidMinGoodwill, -100, 100);
            transporterCooldownSeconds = Mathf.Clamp(transporterCooldownSeconds, 0, 300);
        }

        // Helpers
        public int TricorderCooldownTicks => tricorderCooldownSeconds * 60;
        public float AlertVolume01 => alertVolume;
        public int TransporterCooldownTicks => transporterCooldownSeconds * 60;
    }

    public class YASTM_Mod : Mod
    {
        public static YASTM_ModSettings Settings;

        public YASTM_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<YASTM_ModSettings>();
        }

        public override string SettingsCategory() => "YASTM (Star Trek)";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.Label("Promotion objectives multiplier: " + Settings.promotionObjectiveMultiplier.ToString("0.0"));
            Settings.promotionObjectiveMultiplier = ls.Slider(Settings.promotionObjectiveMultiplier, 0.5f, 3.0f);

            ls.Gap(6);
            ls.Label("Min extra days since last promotion: " + Settings.minDaysExtraSinceLastPromotion);
            Settings.minDaysExtraSinceLastPromotion = Mathf.RoundToInt(ls.Slider(Settings.minDaysExtraSinceLastPromotion, 0, 30));

            ls.Gap(6);
            ls.Label("Tricorder shared cooldown (seconds): " + Settings.tricorderCooldownSeconds);
            Settings.tricorderCooldownSeconds = Mathf.RoundToInt(ls.Slider(Settings.tricorderCooldownSeconds, 0, 600));

            ls.Gap(6);
            ls.Label("Transporter beacon max range (tiles): " + Settings.transporterMaxRange);
            Settings.transporterMaxRange = Mathf.RoundToInt(ls.Slider(Settings.transporterMaxRange, 3, 100));

            ls.Gap(6);
            ls.Label("Alert sound volume: " + Settings.alertVolume.ToString("0.00"));
            Settings.alertVolume = ls.Slider(Settings.alertVolume, 0f, 2f);

            ls.Gap(6);
            ls.Label("Max concurrent promotion candidates: " + Settings.promotionMaxConcurrent);
            Settings.promotionMaxConcurrent = Mathf.RoundToInt(ls.Slider(Settings.promotionMaxConcurrent, 1, 5));
            
            ls.Gap(12);
            ls.Label("Starfleet Aid — cooldown (days): " + Settings.AidCooldownDays);
            Settings.AidCooldownDays = Mathf.RoundToInt(ls.Slider(Settings.AidCooldownDays, 0, 30));

            ls.Label("Starfleet Aid — silver cost: " + Settings.AidSilverCost);
            Settings.AidSilverCost = Mathf.RoundToInt(ls.Slider(Settings.AidSilverCost, 0, 10000));

            ls.Label("Starfleet Aid — goodwill cost: " + Settings.AidGoodwillCost);
            Settings.AidGoodwillCost = Mathf.RoundToInt(ls.Slider(Settings.AidGoodwillCost, 0, 100));

            ls.Label("Starfleet Aid — min goodwill: " + Settings.AidMinGoodwill);
            Settings.AidMinGoodwill = Mathf.RoundToInt(ls.Slider(Settings.AidMinGoodwill, -100, 100));
            ls.End();

            Settings.ClampAll();
        }
    }
}
