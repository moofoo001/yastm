// Mods/YASTM/Source/Harmony/Patch_PlayerPrimaryCultureEnforcer.cs
using RimWorld;
using System.Linq;
using System.Reflection;
using Verse;

namespace YASTM.IdeoFix
{
    public class PlayerPrimaryCultureEnforcer : GameComponent
    {
        private int ticks;
        private bool done;

        public PlayerPrimaryCultureEnforcer(Game game) { }

        public override void StartedNewGame()
        {
            ticks = 0; done = false;
            EnforceNow();
        }

        public override void LoadedGame()
        {
            ticks = 0; done = false;
        }

        public override void GameComponentTick()
        {
            if (done || !ModsConfig.IdeologyActive) return;

            ticks++;
            if (ticks % 250 == 0 && ticks <= 5000)
                EnforceNow();

            if (ticks > 5000) done = true;
        }

        private void EnforceNow()
        {
            var player  = Faction.OfPlayer;
            var primary = player?.ideos?.PrimaryIdeo; 
            if (primary == null) return;

            
            CultureDef preferred = DefDatabase<CultureDef>.GetNamedSilentFail("ST_Culture_UFP")
                                  ?? player.def?.allowedCultures?.FirstOrDefault();

            if (preferred != null)
                TrySetIdeoCulture(primary, preferred);

            
            foreach (var p in PawnsFinder.AllMaps_FreeColonists)
            {
                if (!p.RaceProps.Humanlike) continue;
                if (p.ideo == null || p.ideo.Ideo == null)
                    p.ideo?.SetIdeo(primary); 
            }
        }

        private static void TrySetIdeoCulture(Ideo ideo, CultureDef culture)
        {
            
            try
            {
                var prop = typeof(Ideo).GetProperty("culture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop?.CanWrite == true) { prop.SetValue(ideo, culture); return; }

                var fld = typeof(Ideo).GetField("cultureInt", BindingFlags.Instance | BindingFlags.NonPublic)
                          ?? typeof(Ideo).GetField("culture", BindingFlags.Instance | BindingFlags.NonPublic);
                fld?.SetValue(ideo, culture);
            }
            catch
            {
                
            }
        }
    }
}

