using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public enum AlertLevel { Green, Yellow, Red }

    // WICHTIG: Name geändert von MapComponent_AlertPanel zu MapComponent_ColonyAlert
    // Damit findet das Spiel die Klasse wieder!
    public class MapComponent_ColonyAlert : MapComponent
    {
        private AlertLevel currentLevel = AlertLevel.Green;
        private List<CompAlertPanel> registeredPanels = new List<CompAlertPanel>();

        public int redAlertTicksLeft;
        public int yellowAlertTicksLeft;
        public int NextAllowedTickRed;
        public int NextAllowedTickYellow;

        public AlertLevel CurrentLevel => currentLevel;

        public MapComponent_ColonyAlert(Map map) : base(map) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentLevel, "currentLevel", AlertLevel.Green);
            Scribe_Values.Look(ref redAlertTicksLeft, "redAlertTicksLeft", 0);
            Scribe_Values.Look(ref yellowAlertTicksLeft, "yellowAlertTicksLeft", 0);
            Scribe_Values.Look(ref NextAllowedTickRed, "NextAllowedTickRed", 0);
            Scribe_Values.Look(ref NextAllowedTickYellow, "NextAllowedTickYellow", 0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (currentLevel == AlertLevel.Red)
            {
                if (redAlertTicksLeft > 0)
                {
                    redAlertTicksLeft--;
                    if (redAlertTicksLeft <= 0) EndAlert();
                }
            }
            else if (currentLevel == AlertLevel.Yellow)
            {
                if (yellowAlertTicksLeft > 0)
                {
                    yellowAlertTicksLeft--;
                    if (yellowAlertTicksLeft <= 0) EndAlert();
                }
            }
        }

        public void Register(CompAlertPanel panel)
        {
            if (!registeredPanels.Contains(panel)) registeredPanels.Add(panel);
        }

        public void Deregister(CompAlertPanel panel)
        {
            if (registeredPanels.Contains(panel)) registeredPanels.Remove(panel);
        }

        public void StartRedAlert(int duration, int cooldown, int blinkRate)
        {
            if (Find.TickManager.TicksGame < NextAllowedTickRed) return;

            SetAlertLevel(AlertLevel.Red);
            redAlertTicksLeft = duration;
            NextAllowedTickRed = Find.TickManager.TicksGame + cooldown + duration;
        }

        public void StartYellowAlert(int duration, int cooldown, int blinkRate)
        {
            if (Find.TickManager.TicksGame < NextAllowedTickYellow) return;

            SetAlertLevel(AlertLevel.Yellow);
            yellowAlertTicksLeft = duration;
            NextAllowedTickYellow = Find.TickManager.TicksGame + cooldown + duration;
        }

        public void EndAlert()
        {
            SetAlertLevel(AlertLevel.Green);
            redAlertTicksLeft = 0;
            yellowAlertTicksLeft = 0;
        }

        public void SetAlertLevel(AlertLevel newLevel)
        {
            if (currentLevel == newLevel) return;
            currentLevel = newLevel;
            
            PlayAlertSound(newLevel);
            
            for (int i = registeredPanels.Count - 1; i >= 0; i--)
            {
                var panel = registeredPanels[i];
                if (panel == null || panel.parent == null || !panel.parent.Spawned)
                {
                    registeredPanels.RemoveAt(i);
                    continue;
                }
                panel.UpdateVisuals();
            }

            if (newLevel == AlertLevel.Red)
            {
                Messages.Message("RED ALERT Initiated!", MessageTypeDefOf.ThreatBig, true);
            }
            else if (newLevel == AlertLevel.Yellow)
            {
                Messages.Message("Yellow Alert condition set.", MessageTypeDefOf.CautionInput, false);
            }
            else
            {
                Messages.Message("Condition Green. Stand down.", MessageTypeDefOf.PositiveEvent, false);
            }
        }

        private void PlayAlertSound(AlertLevel level)
        {
            if (level == AlertLevel.Red)
                SoundDef.Named("ST_Sound_RedAlert").PlayOneShotOnCamera(map);
            else if (level == AlertLevel.Yellow)
                SoundDef.Named("ST_Sound_YellowAlert").PlayOneShotOnCamera(map);
        }
    }
}