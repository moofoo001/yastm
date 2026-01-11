// Source/Comps/CompTricorderMedical.cs
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound; 

namespace YASTM
{

    public class CompProperties_TricorderMedical : CompProperties
    {
      
        public int cooldownTicks = 6000;         // 100s
        public int scanTicks = 1200;             // 20s work
        public int range = 12;                   // target range
        public string hediffDef = "ST_MedScan_Boost";
        public int hediffMinTicks = 30000;       // 8,3 Min
        public int hediffMaxTicks = 45000;       // 12,5 Min
        public int minMedicine = 0;              // optional skill level requirement

        public CompProperties_TricorderMedical()
        {
            compClass = typeof(CompTricorderMedical);
        }
    }


    public class CompTricorderMedical : ThingComp
    {
        public CompProperties_TricorderMedical Props => (CompProperties_TricorderMedical)props;

        private int nextUseTick;


        public int Range => Props?.range ?? 0;
        public int ScanTicks => Props?.scanTicks ?? 0;
        public int CooldownTicks => Props?.cooldownTicks ?? 0;
        public bool OnCooldown => Find.TickManager.TicksGame < nextUseTick;
        public int CooldownRemainingTicks => Mathf.Max(0, nextUseTick - Find.TickManager.TicksGame);


        public string HediffDefName => Props?.hediffDef ?? string.Empty; 
        public int HediffDuration
            => Mathf.Max(Props?.hediffMinTicks ?? 0, Props?.hediffMaxTicks ?? 0); 

        
        public HediffDef HediffDef => string.IsNullOrEmpty(Props?.hediffDef)
            ? null
            : DefDatabase<HediffDef>.GetNamedSilentFail(Props.hediffDef);

        public int MinMedicine => Mathf.Max(0, Props?.minMedicine ?? 0);

    
        public bool CanUseNow(Pawn user)
        {
            if (user == null || user.Dead || !user.Spawned) return false;
            if (OnCooldown) return false;

            if (MinMedicine > 0)
            {
                int lvl = user.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;
                if (lvl < MinMedicine) return false;
            }
            return true;
        }

 
        public void UseOn(Pawn user, Pawn target)
        {
            if (target == null || target.Destroyed) return;

       
            var sdef = SoundDef.Named("ST_Tricorder");
            var info = SoundInfo.InMap(new TargetInfo(target.Position, target.Map), MaintenanceType.None);
            SoundStarter.PlayOneShot(sdef, info);

       
            var hdef = HediffDef;
            if (hdef != null && target.health != null)
            {
                var h = target.health.AddHediff(hdef);
                if (h.TryGetComp<HediffComp_Disappears>() is HediffComp_Disappears disp)
                {
                    int minT = Mathf.Max(0, Props.hediffMinTicks);
                    int maxT = Mathf.Max(minT, Props.hediffMaxTicks);
                    disp.ticksToDisappear = Rand.RangeInclusive(minT, maxT);
                }
            }

            
            int cd = Mathf.Max(0, Props?.cooldownTicks ?? 0);
            nextUseTick = Find.TickManager.TicksGame + cd;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref nextUseTick, "ST_TricorderMed_nextUseTick", 0);
        }
    }
}

