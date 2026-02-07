using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class CompProperties_Turbolift : CompProperties
    {
        public int cooldownTicks = 600; // Standard: 10 Sekunden Pause nach Nutzung
        public CompProperties_Turbolift()
        {
            this.compClass = typeof(CompTurbolift);
        }
    }

    public class CompTurbolift : ThingComp
    {
        private int cooldownCounter = 0;

        public CompProperties_Turbolift Props => (CompProperties_Turbolift)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref cooldownCounter, "cooldownCounter", 0);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (cooldownCounter > 0)
            {
                cooldownCounter--;
            }
        }

        // Wird vom JobDriver aufgerufen, wenn jemand den Lift benutzt
        public void StartCooldown()
        {
            cooldownCounter = Props.cooldownTicks;
        }

        public bool IsReady => cooldownCounter <= 0;

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.IsColonistPlayerControlled || selPawn.Drafted) yield break;

            // 1. Check: Ist DIESER Lift bereit?
            if (!IsReady)
            {
                yield return new FloatMenuOption($"Turbolift active (Wait {cooldownCounter / 60}s)", null);
                yield break;
            }

            var otherLifts = parent.Map.listerBuildings.allBuildingsColonist
                .Where(b => b.def == parent.def && b != parent && b.Spawned)
                .ToList();

            if (otherLifts.Count == 0)
            {
                yield return new FloatMenuOption("No connection established", null);
                yield break;
            }

            foreach (var lift in otherLifts)
            {
                string destName = lift.GetRoom()?.Role?.LabelCap ?? "Unknown Deck";
                
                // 2. Check: Ist das ZIEL bereit?
                var destComp = lift.GetComp<CompTurbolift>();
                if (destComp != null && !destComp.IsReady)
                {
                     yield return new FloatMenuOption($"Dest: {destName} (Busy)", null);
                     continue;
                }

                string label = $"Turbolift to: {destName}";

                yield return new FloatMenuOption(label, () =>
                {
                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ST_Job_UseTurbolift"), parent, lift);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }
        
        public override string CompInspectStringExtra()
        {
            if (cooldownCounter > 0)
            {
                return $"Status: Moving / Resetting ({cooldownCounter / 60}s)";
            }
            return "Status: Ready";
        }
    }
}