// Source/Map/MapComponent_AlertPanel.cs
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class MapComponent_AlertPanel : MapComponent
    {
        // Zustände
        public bool IsRedAlertOn;
        public bool IsYellowAlertOn;

        // Blink/Cooldown
        public int BlinkUntilTickRed;
        public int BlinkUntilTickYellow;
        public int NextAllowedTickRed;
        public int NextAllowedTickYellow;

        // Auto-Stop (optional)
        public int RedEndTick;
        public int YellowEndTick;

        // Laufende Sustainer
        private Sustainer redSiren;
        private Sustainer yellowSiren;

        public MapComponent_AlertPanel(Map map) : base(map) { }

        // ---------------- API: von Gizmo gerufen ----------------

        public void StartRedAlert(int blinkSeconds, int durationTicks)
        {
            // State
            IsYellowAlertOn = false;
            StopYellowSiren();

            IsRedAlertOn = true;
            BlinkUntilTickRed = Find.TickManager.TicksGame + Mathf.Max(1, blinkSeconds) * 60;
            RedEndTick = durationTicks > 0 ? Find.TickManager.TicksGame + durationTicks : 0;

            // SFX
            PlayOneShot(SoundDef.Named("ST_RedAlert_Chirp"));
            StartRedSiren();

            // Effekte
            ApplyAlertEffectsImmediate();

            // Letter
            TrySendLetter("ST.AlertPanel.Red.LetterLabel".Translate(),
                          "ST.AlertPanel.Red.LetterText".Translate(),
                          LetterDefOf.ThreatSmall);
        }

        public void StopRedAlert()
        {
            IsRedAlertOn = false;
            RedEndTick = 0;
            StopRedSiren();
            RemoveAlertHediffs();

            TrySendLetter("ST.AlertPanel.RedOff.LetterLabel".Translate(),
                          "ST.AlertPanel.RedOff.LetterText".Translate(),
                          LetterDefOf.NeutralEvent);
        }

        public void StartYellowAlert(int blinkSeconds, int durationTicks)
        {
            IsRedAlertOn = false;
            StopRedSiren();

            IsYellowAlertOn = true;
            BlinkUntilTickYellow = Find.TickManager.TicksGame + Mathf.Max(1, blinkSeconds) * 60;
            YellowEndTick = durationTicks > 0 ? Find.TickManager.TicksGame + durationTicks : 0;

            PlayOneShot(SoundDef.Named("ST_YellowAlert_Chirp"));
            StartYellowSiren();

            ApplyAlertEffectsImmediate();

            TrySendLetter("ST.AlertPanel.Yellow.LetterLabel".Translate(),
                          "ST.AlertPanel.Yellow.LetterText".Translate(),
                          LetterDefOf.PositiveEvent);
        }

        public void StopYellowAlert()
        {
            IsYellowAlertOn = false;
            YellowEndTick = 0;
            StopYellowSiren();
            RemoveAlertHediffs();

            TrySendLetter("ST.AlertPanel.YellowOff.LetterLabel".Translate(),
                          "ST.AlertPanel.YellowOff.LetterText".Translate(),
                          LetterDefOf.NeutralEvent);
        }

        // Cooldown-API (nur fürs Einschalten relevant)
        public bool IsRedOnCooldown()    => Find.TickManager.TicksGame < NextAllowedTickRed;
        public bool IsYellowOnCooldown() => Find.TickManager.TicksGame < NextAllowedTickYellow;
        public void ArmRedCooldownSeconds(int seconds)    => NextAllowedTickRed    = Find.TickManager.TicksGame + Mathf.Max(1, seconds) * 60;
        public void ArmYellowCooldownSeconds(int seconds) => NextAllowedTickYellow = Find.TickManager.TicksGame + Mathf.Max(1, seconds) * 60;

        // ---------------- Sirenen ----------------

        private void StartRedSiren()
        {
            StopYellowSiren();
            if (redSiren == null || redSiren.Ended)
            {
                var info = SoundInfo.InMap(new TargetInfo(map.Center, map), MaintenanceType.PerTick);
                info.volumeFactor = Mathf.Clamp01(YASTM_Mod.Settings?.AlertVolume01 ?? 1f);
                redSiren = SoundDef.Named("ST_RedAlert_SirenLoop").TrySpawnSustainer(info);
            }
        }

        private void StopRedSiren()
        {
            if (redSiren != null && !redSiren.Ended) redSiren.End();
            redSiren = null;
        }

        private void StartYellowSiren()
        {
            StopRedSiren();
            if (yellowSiren == null || yellowSiren.Ended)
            {
                var info = SoundInfo.InMap(new TargetInfo(map.Center, map), MaintenanceType.PerTick);
                info.volumeFactor = Mathf.Clamp01(YASTM_Mod.Settings?.AlertVolume01 ?? 1f);
                yellowSiren = SoundDef.Named("ST_YellowAlert_SirenLoop").TrySpawnSustainer(info);
            }
        }

        private void StopYellowSiren()
        {
            if (yellowSiren != null && !yellowSiren.Ended) yellowSiren.End();
            yellowSiren = null;
        }

        private void PlayOneShot(SoundDef sound)
        {
            if (sound == null) return;
            var info = SoundInfo.InMap(new TargetInfo(map.Center, map), MaintenanceType.None);
            info.volumeFactor = Mathf.Clamp01(YASTM_Mod.Settings?.AlertVolume01 ?? 1f);
            SoundStarter.PlayOneShot(sound, info);
        }

        // ---------------- Effekte ----------------

        public void ApplyAlertEffectsImmediate()
        {
            RemoveAlertHediffs();
            ApplyAlertHediffs();
        }

        private void ApplyAlertHediffs()
        {
            var pawns = map.mapPawns?.FreeColonistsSpawned?.ToList();
            if (pawns == null || pawns.Count == 0) return;

            HediffDef red = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Alert_RedState");
            HediffDef yellow = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Alert_YellowState");

            foreach (var p in pawns)
            {
                if (p?.health == null) continue;

                // aufräumen
                TryRemoveAll(p, red);
                TryRemoveAll(p, yellow);

                if (IsRedAlertOn && red != null) p.health.AddHediff(red);
                else if (IsYellowAlertOn && yellow != null) p.health.AddHediff(yellow);
            }
        }

        private void RemoveAlertHediffs()
        {
            var pawns = map.mapPawns?.FreeColonistsSpawned?.ToList();
            if (pawns == null || pawns.Count == 0) return;

            HediffDef red = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Alert_RedState");
            HediffDef yellow = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Alert_YellowState");

            foreach (var p in pawns)
            {
                TryRemoveAll(p, red);
                TryRemoveAll(p, yellow);
            }
        }

        private static void TryRemoveAll(Pawn p, HediffDef def)
        {
            if (p == null || def == null) return;
            var list = p.health.hediffSet.hediffs;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].def == def) p.health.RemoveHediff(list[i]);
        }

        private void TrySendLetter(string label, string text, LetterDef type)
        {
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(text)) return;
            Find.LetterStack.ReceiveLetter(label, text, type, new TargetInfo(map.Center, map));
        }

        // ---------------- Tick ----------------

        public override void MapComponentTick()
        {
            // Sustain-Sirenen warten
            if (IsRedAlertOn)    redSiren?.Maintain();
            if (IsYellowAlertOn) yellowSiren?.Maintain();

            int now = Find.TickManager.TicksGame;

            // Blink-Timer
            if (BlinkUntilTickRed > 0 && now >= BlinkUntilTickRed) BlinkUntilTickRed = 0;
            if (BlinkUntilTickYellow > 0 && now >= BlinkUntilTickYellow) BlinkUntilTickYellow = 0;

            // Auto-Timeout
            if (IsRedAlertOn && RedEndTick > 0 && now >= RedEndTick)
                StopRedAlert();
            if (IsYellowAlertOn && YellowEndTick > 0 && now >= YellowEndTick)
                StopYellowAlert();
        }

        // ---------------- Save/Load ----------------

        public override void ExposeData()
        {
            Scribe_Values.Look(ref IsRedAlertOn, "ST_IsRedAlertOn", false);
            Scribe_Values.Look(ref IsYellowAlertOn, "ST_IsYellowAlertOn", false);
            Scribe_Values.Look(ref BlinkUntilTickRed, "ST_BlinkUntilTickRed", 0);
            Scribe_Values.Look(ref BlinkUntilTickYellow, "ST_BlinkUntilTickYellow", 0);
            Scribe_Values.Look(ref NextAllowedTickRed, "ST_NextAllowedTickRed", 0);
            Scribe_Values.Look(ref NextAllowedTickYellow, "ST_NextAllowedTickYellow", 0);
            Scribe_Values.Look(ref RedEndTick, "ST_RedEndTick", 0);
            Scribe_Values.Look(ref YellowEndTick, "ST_YellowEndTick", 0);
        }
    }
}
