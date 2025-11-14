// File: Source/Map/MapComponent_AlertPanel.cs
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound; 

namespace YASTM
{
    public class MapComponent_AlertPanel : MapComponent
    {
        public bool IsRedAlertOn;
        public bool IsYellowAlertOn;

        public int BlinkUntilTickRed;
        public int BlinkUntilTickYellow;

        public int NextAllowedTickRed;
        public int NextAllowedTickYellow;

        public int RedEndTick;
        public int YellowEndTick;

        // Optional: Sustain-Sirenen aus ST_Sounds.xml, falls vorhanden
        private Sustainer redSirenSustainer;
        private Sustainer yellowSirenSustainer;

        public MapComponent_AlertPanel(Map map) : base(map) { }

        public void StartRedAlert(int durationTicks, int cooldownTicks, int blinkSeconds)
        {
            int now = Find.TickManager.TicksGame;
            if (now < NextAllowedTickRed) {
                Messages.Message($"Red Alert ist auf Cooldown ({(NextAllowedTickRed - now)/60}s).", MessageTypeDefOf.RejectInput, historical:false);
                return;
            }

            IsRedAlertOn = true;
            RedEndTick = now + durationTicks;
            NextAllowedTickRed = RedEndTick + cooldownTicks;
            BlinkUntilTickRed = now + blinkSeconds * 60;

            // Effekte
            PlayOneShotOnMap("ST_SFX_RedAlert"); // fallback-OneShot
            TryStartSustainer(ref redSirenSustainer, "ST_SFX_RedAlertSiren");

            Messages.Message("Red Alert aktiviert!", MessageTypeDefOf.ThreatBig, historical:false);
            ApplyAlertHediff("ST_Alert_RedState");
        }

        public void StopRedAlert()
        {
            IsRedAlertOn = false;
            RedEndTick = 0;
            TryStopSustainer(ref redSirenSustainer);
            RemoveAlertHediff("ST_Alert_RedState");
        }

        public void StartYellowAlert(int durationTicks, int cooldownTicks, int blinkSeconds)
        {
            int now = Find.TickManager.TicksGame;
            if (now < NextAllowedTickYellow) {
                Messages.Message($"Yellow Alert ist auf Cooldown ({(NextAllowedTickYellow - now)/60}s).", MessageTypeDefOf.RejectInput, historical:false);
                return;
            }

            IsYellowAlertOn = true;
            YellowEndTick = now + durationTicks;
            NextAllowedTickYellow = YellowEndTick + cooldownTicks;
            BlinkUntilTickYellow = now + blinkSeconds * 60;

            PlayOneShotOnMap("ST_SFX_YellowAlert");
            TryStartSustainer(ref yellowSirenSustainer, "ST_SFX_YellowAlertSiren");

            Messages.Message("Yellow Alert aktiviert!", MessageTypeDefOf.NeutralEvent, historical:false);
            ApplyAlertHediff("ST_Alert_YellowState");
        }

        public void StopYellowAlert()
        {
            IsYellowAlertOn = false;
            YellowEndTick = 0;
            TryStopSustainer(ref yellowSirenSustainer);
            RemoveAlertHediff("ST_Alert_YellowState");
        }

        public bool IsRedOnCooldown    => Find.TickManager.TicksGame < NextAllowedTickRed;
        public bool IsYellowOnCooldown => Find.TickManager.TicksGame < NextAllowedTickYellow;

        public int ArmRedCooldownSeconds()    => IsRedOnCooldown ? (NextAllowedTickRed    - Find.TickManager.TicksGame) / 60 : 0;
        public int ArmYellowCooldownSeconds() => IsYellowOnCooldown ? (NextAllowedTickYellow - Find.TickManager.TicksGame) / 60 : 0;

        public override void MapComponentTick()
        {
            if ((Find.TickManager.TicksGame % 60) != 0) return; // 1x/Sek.

            int now = Find.TickManager.TicksGame;
            if (IsRedAlertOn && now >= RedEndTick) StopRedAlert();
            if (IsYellowAlertOn && now >= YellowEndTick) StopYellowAlert();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref IsRedAlertOn,        "IsRedAlertOn");
            Scribe_Values.Look(ref IsYellowAlertOn,     "IsYellowAlertOn");
            Scribe_Values.Look(ref BlinkUntilTickRed,   "BlinkUntilTickRed");
            Scribe_Values.Look(ref BlinkUntilTickYellow,"BlinkUntilTickYellow");
            Scribe_Values.Look(ref NextAllowedTickRed,  "NextAllowedTickRed");
            Scribe_Values.Look(ref NextAllowedTickYellow,"NextAllowedTickYellow");
            Scribe_Values.Look(ref RedEndTick,          "RedEndTick");
            Scribe_Values.Look(ref YellowEndTick,       "YellowEndTick");
        }

        // --- helpers ---
        void ApplyAlertHediff(string defName)
        {
            var hd = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (hd == null) return;

            foreach (var p in map.mapPawns.FreeColonistsSpawned)
                if (!p.health.hediffSet.HasHediff(hd))
                    p.health.AddHediff(hd);
        }

        void RemoveAlertHediff(string defName)
        {
            var hd = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (hd == null) return;
            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                var h = p.health.hediffSet.hediffs.FirstOrDefault(x => x.def == hd);
                if (h != null) p.health.RemoveHediff(h);
            }
        }

        void PlayOneShotOnMap(string soundDefName)
        {
            var s = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
            if (s != null) s.PlayOneShot(SoundInfo.OnCamera(MaintenanceType.None));
        }

        void TryStartSustainer(ref Sustainer sust, string soundDefName)
        {
            if (sust != null) return;
            var sDef = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
            if (sDef != null) sust = sDef.TrySpawnSustainer(SoundInfo.OnCamera(MaintenanceType.PerTick));
        }

        void TryStopSustainer(ref Sustainer sust)
        {
            if (sust == null) return;
            sust.End();
            sust = null;
        }
    }
}
