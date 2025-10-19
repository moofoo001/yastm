using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class CompProperties_TricorderScience : CompProperties
    {
        public int cooldownTicks = 6000;   // ~2h
        public int scanTicks = 900;        // ~15s
        public float range = 18f;          // Scanradius ab Zielzelle
        public string hediffDef = "ST_ScienceInsight";
        public int hediffMinTicks = 30000; // 0.5d
        public int hediffMaxTicks = 45000; // 0.75d
        public int minIntellectual = 4;    // Mind. Forschen 4

        public CompProperties_TricorderScience() { compClass = typeof(CompTricorderScience); }
    }

    public class CompTricorderScience : ThingComp
    {
        public CompProperties_TricorderScience Props => (CompProperties_TricorderScience)props;
        private int nextAllowedTick;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAllowedTick, "ST_TricorderSci_next", 0);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            var apparel = parent as Apparel;
            var wearer  = apparel?.Wearer;
            if (wearer == null || wearer.Faction != Faction.OfPlayer) yield break;

            var cmd = new Command_Target
            {
                defaultLabel   = "ST.Tricorder.Sci.Scan".Translate(),
                defaultDesc    = "ST.Tricorder.Sci.Scan.Desc".Translate(),
                icon           = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ScienceScan", false),
                targetingParams = new TargetingParameters { canTargetLocations = true, canTargetPawns = false }
            };
            int now = Find.TickManager.TicksGame;

            if (now < nextAllowedTick)
                cmd.Disable("ST.Common.Recharging".Translate((nextAllowedTick - now).ToStringTicksToPeriod()));

            int intel = wearer.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
            if (intel < Props.minIntellectual)
                cmd.Disable("ST.Tricorder.Sci.SkillReq".Translate(Props.minIntellectual));

            cmd.action = t => TryStartScan(wearer, t.Cell);
            yield return cmd;
        }

        private void TryStartScan(Pawn user, IntVec3 cell)
        {
            int now = Find.TickManager.TicksGame;
            if (now < nextAllowedTick) return;

            if (!cell.InBounds(user.Map) || cell.DistanceTo(user.Position) > Props.range)
            {
                Messages.Message("ST.Tricorder.Sci.OutOfRange".Translate(), user, MessageTypeDefOf.RejectInput);
                return;
            }

            var jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ST_ScanAreaScience");
            if (jobDef == null)
            {
                Messages.Message("Missing JobDef: ST_ScanAreaScience", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            var job = new Job(jobDef, cell);
            user.jobs.TryTakeOrderedJob(job);
            nextAllowedTick = now + Props.cooldownTicks;
        }

        // Für den Job:
        public int ScanTicks => Props.scanTicks;
        public float ScanRange => Props.range;
        public string HediffDefName => Props.hediffDef;
        public IntRange HediffDuration => new IntRange(Props.hediffMinTicks, Props.hediffMaxTicks);
    }
}
