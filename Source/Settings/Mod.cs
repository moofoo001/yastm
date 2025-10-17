using RimWorld;
using UnityEngine;
using Verse;

namespace YASTM
{
    public class YASTM_Mod : Mod
    {
        public static YASTM_Settings Settings;

        public YASTM_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<YASTM_Settings>();
        }

        public override string SettingsCategory() => "YASTM – Starfleet Aid";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var s = Settings;
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.Label($"Cooldown (days): {s.AidCooldownDays:F1}");
            s.AidCooldownDays = Mathf.Round(ls.Slider(s.AidCooldownDays, 1f, 20f) * 10f) / 10f;

            ls.GapLine();

            ls.Label($"Silver cost: {s.AidSilverCost}");
            s.AidSilverCost = Mathf.RoundToInt(ls.Slider(s.AidSilverCost, 0, 800));

            ls.Label($"Goodwill cost: {s.AidGoodwillCost}");
            s.AidGoodwillCost = Mathf.RoundToInt(ls.Slider(s.AidGoodwillCost, 0, 20));

            ls.Label($"Min goodwill required: {s.AidMinGoodwill}");
            s.AidMinGoodwill = Mathf.RoundToInt(ls.Slider(s.AidMinGoodwill, 0, 70));

            ls.End();
        }
    }
}
