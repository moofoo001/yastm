using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI; 
using UnityEngine;

namespace YASTM
{
    public class CompProperties_BadgeyShutdown : CompProperties
    {
        public CompProperties_BadgeyShutdown()
        {
            this.compClass = typeof(CompBadgeyShutdown);
        }
    }

    public class CompBadgeyShutdown : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            var badgey = parent.Map.GetComponent<MapComponent_BadgeyChaos>();
            if (badgey != null && badgey.IsActive)
            {
                yield return new Command_Action
                {
                    defaultLabel = "PURGE BADGEY AI",
                    defaultDesc = "Attempt to delete the malicious hologram program.",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/Database", true),
                    action = () =>
                    {
                        Pawn actor = Find.Selector.SingleSelectedThing as Pawn;
                        if (actor != null && actor.IsColonist)
                        {
                           // Gizmo Instant Action ( optional job)
                           DoPurge(actor);
                        }
                        else
                        {
                            Messages.Message("Select an engineer (Pawn) and right-click the console to purge!", MessageTypeDefOf.RejectInput, false);
                        }
                    }
                };
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            var badgey = parent.Map.GetComponent<MapComponent_BadgeyChaos>();
            if (badgey != null && badgey.IsActive)
            {
                yield return new FloatMenuOption("Purge Badgey AI (Hacking)", () =>
                {
                    //  use our own job
                    if (ST_JobDefOf.ST_Job_PurgeBadgey != null)
                    {
                        Job job = JobMaker.MakeJob(ST_JobDefOf.ST_Job_PurgeBadgey, parent);
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                });
            }
        }

        // This method is called by the JobDriver
        public void DoPurge(Pawn pawn)
        {
            var badgey = parent.Map.GetComponent<MapComponent_BadgeyChaos>();
            if (badgey == null || !badgey.IsActive) return;

            if (pawn.skills.GetSkill(SkillDefOf.Intellectual).Level >= 5)
            {
                badgey.PurgeSystem(pawn);
            }
            else
            {
                Messages.Message($"{pawn.LabelShort} failed to bypass Badgey's firewall! (Needs Intellectual 5+)", MessageTypeDefOf.NegativeEvent);
                FleckMaker.ThrowSmoke(parent.Position.ToVector3Shifted(), parent.Map, 2f);
            }
        }
    }
}