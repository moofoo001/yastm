// in CompTricorderSharedCooldown.cs
using UnityEngine;
using Verse;

namespace YASTM
{
    public class CompProperties_TricorderSharedCooldown : CompProperties
    {
        public CompProperties_TricorderSharedCooldown()
        {
            compClass = typeof(CompTricorderSharedCooldown);
        }
    }

    public class CompTricorderSharedCooldown : ThingComp
    {
        public int nextAllowedTick;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAllowedTick, "ST_TricorderShared_next", 0);
        }

        
        public static int CurrentCooldownTicks =>
            YASTM_Mod.Settings?.TricorderCooldownTicks ?? (30 * 60); 

        public bool IsReady(int now) => now >= nextAllowedTick;

        public void StartCooldown(int now, int cooldownTicks)
        {
            nextAllowedTick = now + cooldownTicks;
        }

      
        public void StartCooldownNow()
        {
            StartCooldown(Find.TickManager.TicksGame, CurrentCooldownTicks);
        }

        public int Remaining(int now) => nextAllowedTick - now;
    }
}

