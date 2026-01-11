using System.Collections.Generic;
using System.Reflection; 
using RimWorld;
using Verse;
using Verse.Sound;     
using UnityEngine;

namespace YASTM
{
    public class MapComponent_AlertPanel : MapComponent
    {
        public int redAlertTicksLeft;
        public int yellowAlertTicksLeft;
        public int redCooldownTicksLeft;
        public int yellowCooldownTicksLeft;

        private Sustainer sirenSustainer;

        public bool IsRedOnCooldown => redCooldownTicksLeft > 0;
        public bool IsYellowOnCooldown => yellowCooldownTicksLeft > 0;
        public int ArmRedCooldownSeconds() => redCooldownTicksLeft / 60;
        public int ArmYellowCooldownSeconds() => yellowCooldownTicksLeft / 60;
        
        public int NextAllowedTickRed => Find.TickManager.TicksGame + redCooldownTicksLeft;
        public int NextAllowedTickYellow => Find.TickManager.TicksGame + yellowCooldownTicksLeft;

        public bool IsRedAlertActive => redAlertTicksLeft > 0; 

        public MapComponent_AlertPanel(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref redAlertTicksLeft, "redAlertTicksLeft");
            Scribe_Values.Look(ref yellowAlertTicksLeft, "yellowAlertTicksLeft");
            Scribe_Values.Look(ref redCooldownTicksLeft, "redCooldownTicksLeft");
            Scribe_Values.Look(ref yellowCooldownTicksLeft, "yellowCooldownTicksLeft");
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (redAlertTicksLeft > 0)
            {
                redAlertTicksLeft--;
                if (redAlertTicksLeft == 0) EndAlert();
                
                if (sirenSustainer != null && !sirenSustainer.Ended)
                {
                    sirenSustainer.Maintain();
                }
            }

            if (yellowAlertTicksLeft > 0)
            {
                yellowAlertTicksLeft--;
                if (yellowAlertTicksLeft == 0) EndAlert();
            }

            if (redCooldownTicksLeft > 0) redCooldownTicksLeft--;
            if (yellowCooldownTicksLeft > 0) yellowCooldownTicksLeft--;
        }

        public void StartRedAlert(int duration, int cooldown, int blink)
        {
            yellowAlertTicksLeft = 0;
            redAlertTicksLeft = duration;
            redCooldownTicksLeft = cooldown;

            StartSiren("ST_SFX_RedAlert");

            SetShields(true);
            SetDoorsLockdown(true);
            DraftCrew(true);
            
            Messages.Message("RED ALERT Engaged!", MessageTypeDefOf.ThreatBig);
        }

        public void StartYellowAlert(int duration, int cooldown, int blink)
        {
            redAlertTicksLeft = 0;
            yellowAlertTicksLeft = duration;
            yellowCooldownTicksLeft = cooldown;

            SoundDef.Named("ST_SFX_YellowAlert")?.PlayOneShotOnCamera(map);

            SetShields(true);
            SetDoorsLockdown(true);
        }

        public void EndAlert()
        {
            redAlertTicksLeft = 0;
            yellowAlertTicksLeft = 0;

            if (sirenSustainer != null && !sirenSustainer.Ended)
            {
                sirenSustainer.End();
                sirenSustainer = null;
            }

            SetShields(false);
            SetDoorsLockdown(false);
            DraftCrew(false);
            
            Messages.Message("Condition Green.", MessageTypeDefOf.PositiveEvent);
        }

        private void StartSiren(string defName)
        {
            if (sirenSustainer != null) sirenSustainer.End();

            SoundDef def = SoundDef.Named(defName);
            if (def != null && def.sustain)
            {
                SoundInfo info = SoundInfo.OnCamera(MaintenanceType.PerTick);
                sirenSustainer = def.TrySpawnSustainer(info);
            }
            else if (def != null)
            {
                def.PlayOneShotOnCamera(map);
            }
        }

        private void SetDoorsLockdown(bool active)
        {
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                if (b is Building_Door door)
                {
                    if (active)
                    {
                        typeof(Building_Door).GetField("holdOpenInt", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.SetValue(door, false);
                        if (door.Open) door.StartManualCloseBy(null);
                    }
                }
            }
        }

        private void SetShields(bool active)
        {
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                // check for shield comp
                var shield = b.TryGetComp<CompProjectileInterceptor>();
                if (shield != null)
                {
                    var flick = b.TryGetComp<CompFlickable>();
                    if (flick != null) 
                    {
                        flick.SwitchIsOn = active;
                        // ensure state is applied
                    }
                }
            }
        }

        private void DraftCrew(bool active)
        {
            foreach (Pawn p in map.mapPawns.FreeColonists)
            {
                if (p.drafter != null && !p.WorkTagIsDisabled(WorkTags.Violent))
                {
                    if (p.drafter.Drafted != active)
                    {
                        if (active && (p.InMentalState || p.Downed)) continue;
                        p.drafter.Drafted = active;
                    }
                }
            }
        }
    }
}