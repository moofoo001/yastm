using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class CompProperties_TricorderMedical : CompProperties
    {
        public int cooldownTicks = 6000;     // ~2 Stunden
        public int scanTicks = 1200;         // ~20s
        public float range = 12f;
        public string hediffDef = "ST_MedScan_Boost";
        public int hediffMinTicks = 30000;   // 0.5 Tage
        public int hediffMaxTicks = 45000;   // 0.75 Tage
        public int minMedicine = 4;

        public CompProperties_TricorderMedical()
        {
            compClass = typeof(CompTricorderMedical);
        }
    }

    public class CompTricorderMedical : ThingComp
    {
        public CompProperties_TricorderMedical Props => (CompProperties_TricorderMedical)props;
        private int nextAllowedTick;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAllowedTick, "ST_TricorderMed_next", 0);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            var apparel = parent as Apparel;
            var wearer  = apparel?.Wearer;
            if (wearer == null || wearer.Faction != Faction.OfPlayer) yield break;

            var cmd = new Command_Target
            {
                defaultLabel = "ST.Tricorder.Med.Scan".Translate(),
                defaultDesc  = "ST.Tricorder.Med.Scan.Desc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/MedScan", false),
                targetingParams = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetBuildings = false,
                    canTargetAnimals = true,
                    canTargetHumans = true,
                    validator = t =>
                    {
                        var p = t.Thing as Pawn;
                        if (p == null) return false;
                        if (!p.RaceProps.Humanlike && !p.RaceProps.Animal) return false;
                        if (p.Dead) return false;
                        if (p.Faction != null && p.Faction.HostileTo(Faction.OfPlayer)) return false;
                        return true;
                    }
                },
                action = t => TryStartScan(wearer, t.Thing as Pawn)
            };

            int now = Find.TickManager.TicksGame;
            var shared = parent.TryGetComp<CompTricorderSharedCooldown>();
            bool gated = shared != null && !shared.IsReady(now);
            if (gated)
                cmd.Disable("ST.Common.Recharging".Translate(shared.Remaining(now).ToStringTicksToPeriod()));

            int med = wearer.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;
            if (med < Props.minMedicine)
                cmd.Disable("ST.Tricorder.Med.SkillReq".Translate(Props.minMedicine));

            yield return cmd;
        }

        private void TryStartScan(Pawn user, Pawn target)
        {
            if (target == null) return;

            int now = Find.TickManager.TicksGame;
            if (now < nextAllowedTick) return;

            if (user.Position.DistanceTo(target.Position) > Props.range)
            {
                Messages.Message("ST.Tricorder.Med.OutOfRange".Translate(), user, MessageTypeDefOf.RejectInput);
                return;
            }

            var jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ST_ScanAllyMedical");
            if (jobDef == null)
            {
                Messages.Message("Missing JobDef: ST_ScanAllyMedical", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            var job = new Job(jobDef, target);
            user.jobs.TryTakeOrderedJob(job);
            nextAllowedTick = now + Props.cooldownTicks;
        }

        // von Job aus abrufbar
        public int ScanTicks => Props.scanTicks;
        public string HediffDefName => Props.hediffDef;
        public IntRange HediffDuration => new IntRange(Props.hediffMinTicks, Props.hediffMaxTicks);
    }
}
