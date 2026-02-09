using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;

namespace YASTM
{
    public enum ST_AlertLevel
    {
        Normal,
        Yellow,
        Red
    }

    public class ST_ColonyAlertSystem : MapComponent
    {
        private ST_AlertLevel currentLevel = ST_AlertLevel.Normal;
        private Sustainer activeSustainer;

        public ST_AlertLevel CurrentLevel => currentLevel;

        public ST_ColonyAlertSystem(Map map) : base(map)
        {
        }

        public void SetAlertLevel(ST_AlertLevel newLevel)
        {
            if (currentLevel == newLevel) return;

            // 1. Sound ändern
            StopSustainer();
            currentLevel = newLevel;

            if (newLevel == ST_AlertLevel.Red)
            {
                ST_SoundDefOf.ST_Sound_RedAlert?.PlayOneShotOnCamera(map);
                StartSustainer(ST_SoundDefOf.ST_Sound_RedAlert_Loop);
                Messages.Message("RED ALERT! Shields up! Crew to combat stations!", MessageTypeDefOf.ThreatBig);
            }
            else if (newLevel == ST_AlertLevel.Yellow)
            {
                ST_SoundDefOf.ST_Sound_YellowAlert?.PlayOneShotOnCamera(map);
                StartSustainer(ST_SoundDefOf.ST_Sound_YellowAlert_Loop);
                Messages.Message("Yellow Alert initiated. Heightened readiness.", MessageTypeDefOf.CautionInput);
            }
            else
            {
                Messages.Message("Condition Green. Stand down.", MessageTypeDefOf.PositiveEvent);
            }

            // 2. Hediffs verteilen (Buffs an Crew geben)
            ApplyAlertBuffs(newLevel);
        }

        private void ApplyAlertBuffs(ST_AlertLevel level)
        {
            // Wir holen alle freien Kolonisten auf der Karte
            List<Pawn> crew = map.mapPawns.FreeColonists;

            foreach (Pawn p in crew)
            {
                if (p.Dead || p.Downed) continue;

                // A) Erst mal ALLES Alte entfernen, damit sich nichts stapelt
                var oldRed = p.health.hediffSet.GetFirstHediffOfDef(ST_HediffDefOf.ST_Alert_RedState);
                if (oldRed != null) p.health.RemoveHediff(oldRed);

                var oldYellow = p.health.hediffSet.GetFirstHediffOfDef(ST_HediffDefOf.ST_Alert_YellowState);
                if (oldYellow != null) p.health.RemoveHediff(oldYellow);

                // B) Neuen Buff hinzufügen (falls nicht Normal)
                if (level == ST_AlertLevel.Red)
                {
                    p.health.AddHediff(ST_HediffDefOf.ST_Alert_RedState);
                }
                else if (level == ST_AlertLevel.Yellow)
                {
                    p.health.AddHediff(ST_HediffDefOf.ST_Alert_YellowState);
                }
            }
        }

        private void StartSustainer(SoundDef loopDef)
        {
            if (loopDef != null)
            {
                SoundInfo info = SoundInfo.OnCamera(MaintenanceType.PerTick);
                activeSustainer = loopDef.TrySpawnSustainer(info);
            }
        }

        private void StopSustainer()
        {
            if (activeSustainer != null && !activeSustainer.Ended)
            {
                activeSustainer.End();
                activeSustainer = null;
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (activeSustainer != null && !activeSustainer.Ended)
            {
                activeSustainer.Maintain();
            }
            
            // Optional: Alle paar Sekunden prüfen, ob neue Pawns dazugekommen sind?

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentLevel, "currentLevel", ST_AlertLevel.Normal);
        }
    }
}