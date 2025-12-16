using UnityEngine;
using Verse;
using RimWorld;

namespace YASTM
{
    public class CompProperties_TransporterPolish : CompProperties
    {
        public CompProperties_TransporterPolish()
        {
           
            this.compClass = typeof(CompTransporterPolish);
        }
    }

    public class CompTransporterPolish : ThingComp
    {
        public int nextAllowedTick;
        public CompProperties_TransporterPolish Props => (CompProperties_TransporterPolish)props;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAllowedTick, "ST_Transporter_next", 0);
        }

        public bool IsReadyNow => Find.TickManager.TicksGame >= nextAllowedTick;

        public void StartCooldownNow()
        {
            int cd = YASTM_Mod.Settings?.TransporterCooldownTicks ?? (10 * 60);
            nextAllowedTick = Find.TickManager.TicksGame + cd;
        }

        public string CooldownLabel()
        {
            int rem = nextAllowedTick - Find.TickManager.TicksGame;
            int sec = Mathf.Max(0, Mathf.CeilToInt(rem / 60f));
            return "ST.Transporter.Cooldown".Translate(sec);
        }

        public bool IsOperational(out string reason)
        {
            reason = null;
            // Power?
            var power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                reason = "ST.Transporter.NeedsPower".Translate();
                return false;
            }
            // Flickable?
            var flick = parent.TryGetComp<CompFlickable>();
            if (flick != null && !flick.SwitchIsOn)
            {
                reason = "ST.Transporter.SwitchedOff".Translate();
                return false;
            }
            return true;
        }
    }
}

