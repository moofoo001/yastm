using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace YASTM
{
    public class MapComponent_BadgeyChaos : MapComponent
    {
        private bool isActive = false;
        private int chaosTick = 0;

        public MapComponent_BadgeyChaos(Map map) : base(map) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isActive, "isActive", false);
        }

        public void TriggerChaos()
        {
            isActive = true;
            
            if (ST_SoundDefOf.ST_Sound_RedAlert != null) 
                ST_SoundDefOf.ST_Sound_RedAlert.PlayOneShotOnCamera(map);
            
            Messages.Message("SECURITY ALERT: Holographic AI 'Badgey' has seized control of base systems!", MessageTypeDefOf.ThreatBig, true);
        }

        public void PurgeSystem(Pawn engineer)
        {
            isActive = false;
            Messages.Message($"{engineer.LabelShort} has successfully deleted Badgey.exe.", MessageTypeDefOf.PositiveEvent, true);
        }

        public bool IsActive => isActive;

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!isActive) return;

            chaosTick++;

            if (chaosTick % 120 == 0)
            {
                DoRandomMischief();
            }
        }

        //  create badgey fleck
        private void ThrowBadgeyFleck(Thing target)
        {
            if (target == null || target.Map == null || ST_FleckDefOf.ST_Fleck_BadgeyOverlay == null) return;

            Vector3 drawPos = target.DrawPos + new Vector3(0, 0, 0.5f);

            // create fleck
            FleckCreationData data = default(FleckCreationData);
            data.def = ST_FleckDefOf.ST_Fleck_BadgeyOverlay;
            data.spawnPosition = drawPos;
            data.scale = 1.2f;
            data.velocityAngle = Rand.Range(80, 100); // up
            data.velocitySpeed = 0.4f;
            data.rotationRate = Rand.Range(-30f, 30f); // glitch-effect

            // create fleck
            target.Map.flecks.CreateFleck(data);
        }

        private void DoRandomMischief()
        {
            int roll = Rand.Range(0, 4);

            switch (roll)
            {
                case 0: // doors
                    List<Building_Door> doors = map.listerBuildings.AllBuildingsColonistOfClass<Building_Door>().ToList();
                    if (doors.Any())
                    {
                        Building_Door door = doors.RandomElement();
                        door.StartManualOpenBy(null); 
                        FleckMaker.ThrowMicroSparks(door.Position.ToVector3Shifted(), map);
                        ThrowBadgeyFleck(door);
                    }
                    break;

                case 1: // lights
                    Building light = map.listerBuildings.allBuildingsColonist
                        .Where(b => b.GetComp<CompFlickable>() != null)
                        .RandomElementWithFallback(null);
                    
                    if (light != null)
                    {
                        CompFlickable flick = light.GetComp<CompFlickable>();
                        flick.SwitchIsOn = !flick.SwitchIsOn; 
                        ThrowBadgeyFleck(light);
                    }
                    break;
                
                case 2: // turrets
                    Building_TurretGun turret = map.listerBuildings.AllBuildingsColonistOfClass<Building_TurretGun>().RandomElementWithFallback(null);
                    
                    bool hasPower = false;
                    var powerComp = turret?.GetComp<CompPowerTrader>();
                    if (powerComp != null && powerComp.PowerOn) hasPower = true;

                    if (turret != null && hasPower)
                    {
                        FleckMaker.ThrowLightningGlow(turret.Position.ToVector3Shifted(), map, 1.0f);
                        turret.TakeDamage(new DamageInfo(DamageDefOf.EMP, 10, 0, -1, null, null, null));
                        ThrowBadgeyFleck(turret);
                    }
                    break;

                case 3: // SPAM
                    if (Rand.Chance(0.2f)) 
                        Messages.Message("Badgey: 'Can I burn your heart in a fire?'", MessageTypeDefOf.NegativeEvent, false);
                    break;
            }
        }
    }
}