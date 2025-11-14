using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class CompProperties_TricorderSecurity : CompProperties
    {
        public int cooldownTicks = 6000;   // ~2h
        public int scanTicks = 900;        // ~15s
        public float range = 18f;          // Zielzelle muss innerhalb liegen
        public string hediffDef = "ST_SecuritySweep";
        public int hediffMinTicks = 30000; // 0.5d
        public int hediffMaxTicks = 45000; // 0.75d
        public int minShooting = 4;        // Mind. Schusswaffen ODER
        public int minMelee = 4;           // Mind. Nahkampf

        public CompProperties_TricorderSecurity() { compClass = typeof(CompTricorderSecurity); }
    }

    public class CompTricorderSecurity : ThingComp
    {
        public CompProperties_TricorderSecurity Props => (CompProperties_TricorderSecurity)props;

        // Fallback (falls Shared-Comp fehlt)
        private int nextAllowedTick;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAllowedTick, "ST_TricorderSec_next", 0);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            var apparel = parent as Apparel;
            var wearer  = apparel?.Wearer;
            if (wearer == null || wearer.Faction != Faction.OfPlayer) yield break;

            var shared = parent.TryGetComp<CompTricorderSharedCooldown>();
            int now = Find.TickManager.TicksGame;
            var cmd = new Command_Target
            {
                defaultLabel   = "ST.Tricorder.Sec.Scan".Translate(),
                defaultDesc    = "ST.Tricorder.Sec.Scan.Desc".Translate(),
                icon           = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/SecurityScan", false),
                targetingParams = new TargetingParameters { canTargetLocations = true, canTargetPawns = false },
                action = t => TryStartScan(wearer, t.Cell)
            };

            // Cooldown-Anzeige (Shared bevorzugt)
            var cd = parent.GetComp<CompTricorderSharedCooldown>();
            if (cd != null && !cd.IsReady(now))
            {
                cmd.Disable("Cooldown: " + Mathf.CeilToInt(cd.Remaining(now)/60f) + " s");
            }
            // Skill-Gate: Shooting ODER Melee
            int shoot = wearer.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
            int melee = wearer.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
            if (shoot < Props.minShooting && melee < Props.minMelee)
                cmd.Disable("ST.Tricorder.Sec.SkillReq".Translate(Props.minShooting, Props.minMelee));

            yield return cmd;
        }

        private void TryStartScan(Pawn user, IntVec3 cell)
        {
            int now = Find.TickManager.TicksGame;
            var shared = parent.TryGetComp<CompTricorderSharedCooldown>();

            // Cooldown-Gate
            if (shared != null)
            {
                if (!shared.IsReady(now))
                {
                    Messages.Message("ST.Common.Recharging".Translate(shared.Remaining(now).ToStringTicksToPeriod()), user, MessageTypeDefOf.RejectInput);
                    return;
                }
            }
            else if (now < nextAllowedTick) return;

            if (!cell.InBounds(user.Map) || cell.DistanceTo(user.Position) > Props.range)
            {
                Messages.Message("ST.Tricorder.Sci.OutOfRange".Translate(), user, MessageTypeDefOf.RejectInput);
                return;
            }

            var jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ST_ScanAreaSecurity");
            if (jobDef == null)
            {
                Messages.Message("Missing JobDef: ST_ScanAreaSecurity", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            var job = new Job(jobDef, cell);
            user.jobs.TryTakeOrderedJob(job);

            // Cooldown START (Shared bevorzugt)
            if (shared != null) shared.StartCooldown(now, Props.cooldownTicks);
            else nextAllowedTick = now + Props.cooldownTicks;
        }

        // Für den Job:
        public int ScanTicks => Props.scanTicks;
        public float ScanRange => Props.range;
        public string HediffDefName => Props.hediffDef;
        public IntRange HediffDuration => new IntRange(Props.hediffMinTicks, Props.hediffMaxTicks);
    }
}
