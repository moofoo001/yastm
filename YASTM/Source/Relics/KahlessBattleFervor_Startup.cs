using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YASTM.Stats
{
    /// <summary>
    /// Attaches the Kahless battle fervor stat part to relevant stats on startup.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class KahlessBattleFervor_Startup
    {
        static KahlessBattleFervor_Startup()
        {
            try
            {
                var part = new StatPart_KahlessBattleFervor();

                AttachPartSafe(StatDefOf.MeleeHitChance, part);
                AttachPartSafe(StatDefOf.MeleeDamageFactor, part);

                Log.Message("[YASTM][Kahless] Battle fervor stat parts attached.");
            }
            catch (System.Exception ex)
            {
                Log.Error("[YASTM][Kahless] Failed to attach battle fervor stat parts: " + ex);
            }
        }

        private static void AttachPartSafe(StatDef stat, StatPart part)
        {
            if (stat == null || part == null)
                return;

            if (stat.parts == null)
                stat.parts = new List<StatPart>();

            // avoid duplicate addition
            foreach (var existing in stat.parts)
            {
                if (existing is StatPart_KahlessBattleFervor)
                    return;
            }

            stat.parts.Add(part);
        }
    }
}
