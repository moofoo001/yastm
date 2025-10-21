using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class CompProperties_KtarianHeadset : CompProperties
    {
        public int sessionTicks = 4000;         // ~1.1h
        public int cooldownTicks = 6000;        // ~2h
        public float obsessionChance = 0.01f;   // 1%

        public CompProperties_KtarianHeadset()
        {
            compClass = typeof(CompKtarianHeadset);
        }
    }

    public class CompKtarianHeadset : ThingComp
    {
        public CompProperties_KtarianHeadset Props => (CompProperties_KtarianHeadset)props;

        private int nextAllowedTick;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAllowedTick, "YASTM_KtarianHeadset_nextAllowedTick", 0);
        }


        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            var apparel = parent as Apparel;
            var wearer  = apparel?.Wearer;
            if (wearer == null || wearer.Faction != Faction.OfPlayer) yield break;

            var cmd = new Command_Action
            {
                defaultLabel = "ST.Ktarian.Play.Button".Translate(),
                defaultDesc  = "ST.Ktarian.Play.Desc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Commands/UseArtifact", false),
                action       = () => TryStartSession(wearer)
            };

            int now = Find.TickManager.TicksGame;

            if (wearer.Downed || wearer.InMentalState)
                cmd.Disable("ST.Ktarian.Play.BlockedState".Translate());
            else if (!wearer.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                cmd.Disable("ST.Ktarian.Play.NeedsTalking".Translate());
            else if (now < nextAllowedTick)
                cmd.Disable("ST.Common.Recharging".Translate((nextAllowedTick - now).ToStringTicksToPeriod()));

            yield return cmd;
        }

        private void TryStartSession(Pawn pawn)
        {
            int now = Find.TickManager.TicksGame;
            if (now < nextAllowedTick) return;

            var jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ST_PlayKtarianHeadset");
            if (jobDef == null)
            {
                Messages.Message("Missing JobDef: ST_PlayKtarianHeadset", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            var job = new Job(jobDef, parent);
            job.count = 1;


            pawn.jobs.TryTakeOrderedJob(job);

            nextAllowedTick = now + Props.cooldownTicks;
        }

        public int SessionTicks => Props.sessionTicks;
        public float ObsessionChance => Props.obsessionChance;
    }
}
