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
        // Zustände (vom Gizmo gesetzt)
        public bool IsRedAlertOn;
        public bool IsYellowAlertOn;

        // Blink/Cooldown (vom Gizmo benutzt)
        public int BlinkUntilTickRed;
        public int BlinkUntilTickYellow;
        public int NextAllowedTickRed;
        public int NextAllowedTickYellow;

        // Sirene (Red)
        private Sustainer redSiren;

        // Hediffs aus XML (ST_Misc_Hediffs.xml)
        private static readonly HediffDef H_Red    = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Alert_RedState");
        private static readonly HediffDef H_Yellow = DefDatabase<HediffDef>.GetNamedSilentFail("ST_Alert_YellowState");

        // optionale Sounds (falls per Code gebraucht)
        private static readonly SoundDef S_RedLoop = DefDatabase<SoundDef>.GetNamedSilentFail("ST_RedAlert_SirenLong");

        // periodisches Sync-Intervall
        private int nextSyncTick;

        public MapComponent_AlertPanel(Map map) : base(map) { }

        // ---------------- Sirenen-API (wird vom Gizmo via Reflection aufgerufen) ----------------

        // Red-Sirene starten (Loop/Sustainer)
        public void StartRedSiren(SoundDef sound)
        {
            StopRedSiren(); // sicherheitshalber vorherigen Sustainer beenden
            var use = sound ?? S_RedLoop;
            if (use != null)
            {
                // Map → TargetInfo (z.B. Mapmitte)
                var ti = new TargetInfo(map.Center, map);
                var info = SoundInfo.InMap(ti);
                info.volumeFactor = Mathf.Clamp01(YASTM_Mod.Settings?.AlertVolume01 ?? 1f);
                redSiren = SoundStarter.TrySpawnSustainer(use, info);
            }
        }

        public void StopRedSiren()
        {
            if (redSiren != null)
            {
                redSiren.End();
                redSiren = null;
            }
        }

        // Optional: kurzer Signal-Ton für Yellow (einmalig)
        
        public void StartYellowSiren(SoundDef sound)
        {
            if (sound == null) return;
            var ti = new TargetInfo(map.Center, map);
            var info = SoundInfo.InMap(ti);
            info.volumeFactor = Mathf.Clamp01(YASTM_Mod.Settings?.AlertVolume01 ?? 1f);
            SoundStarter.PlayOneShot(sound, info);
        }
        // ---------------- Hediff-Verteilung ----------------

        public override void MapComponentTick()
        {
            // Sirene am Leben halten / stoppen
            if (IsRedAlertOn)
            {
                redSiren?.Maintain();
            }
            else
            {
                if (redSiren != null) StopRedSiren();
            }

            // Blink-Optik: Deine existierende Fleck-Logik kann hier laufen (wir lassen sie unverändert)

            // Hediffs regelmäßig synchron halten (z.B. für Neuzugänge)
            if (Find.TickManager.TicksGame >= nextSyncTick)
            {
                ApplyAlertHediffs(map.mapPawns.FreeColonistsSpawned);
                nextSyncTick = Find.TickManager.TicksGame + 250; // ~4s
            }
        }

        /// <summary>
        /// Sofort anwenden (nach Umschalten aufgerufen – kannst du auch manuell callen, wenn du willst).
        /// </summary>
        public void ApplyAlertEffectsImmediate()
        {
            ApplyAlertHediffs(map.mapPawns.FreeColonistsSpawned);
        }

        private static void ApplyAlertHediffs(IEnumerable<Pawn> pawns, bool redOn, bool yellowOn)
        {
            foreach (var p in pawns)
            {
                if (p == null || !p.Spawned) continue;
                TryRemove(p, H_Red);
                TryRemove(p, H_Yellow);

                if (redOn && H_Red != null)
                    TryAddOnce(p, H_Red);
                else if (yellowOn && H_Yellow != null)
                    TryAddOnce(p, H_Yellow);
            }
        }

        private void ApplyAlertHediffs(IEnumerable<Pawn> pawns)
            => ApplyAlertHediffs(pawns, IsRedAlertOn, IsYellowAlertOn);

        private static void TryAddOnce(Pawn p, HediffDef def)
        {
            if (def == null) return;
            if (p.health?.hediffSet?.HasHediff(def) == true) return;
            p.health.AddHediff(def);
        }

        private static void TryRemove(Pawn p, HediffDef def)
        {
            if (def == null) return;
            var h = p.health?.hediffSet?.GetFirstHediffOfDef(def);
            if (h != null) p.health.RemoveHediff(h);
        }

        // ---------------- Speichern/Laden ----------------

        public override void ExposeData()
        {
            Scribe_Values.Look(ref IsRedAlertOn, "ST_IsRedAlertOn", false);
            Scribe_Values.Look(ref IsYellowAlertOn, "ST_IsYellowAlertOn", false);
            Scribe_Values.Look(ref BlinkUntilTickRed, "ST_BlinkUntilTickRed", 0);
            Scribe_Values.Look(ref BlinkUntilTickYellow, "ST_BlinkUntilTickYellow", 0);
            Scribe_Values.Look(ref NextAllowedTickRed, "ST_NextAllowedTickRed", 0);
            Scribe_Values.Look(ref NextAllowedTickYellow, "ST_NextAllowedTickYellow", 0);
        }
    }
}
