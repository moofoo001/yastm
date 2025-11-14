using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YASTM
{
    public class CompProperties_CommsGizmo : CompProperties
    {
        // Kanonisch (bisher im Code benutzt)
        public int aidCooldownDays = 7;
        public string requiredRankTrait = "ST_Rank_Commander";

        // XML-Aliasse (aus deinem Log): <cooldownDays>, <requiredRankTraits>
        public int cooldownDays = -1;
        public List<string> requiredRankTraits;

        public CompProperties_CommsGizmo()
        {
            compClass = typeof(CompCommsGizmo);
        }
    }

    public class CompCommsGizmo : ThingComp
    {
        private CompProperties_CommsGizmo Props => (CompProperties_CommsGizmo)props;

        private int CooldownDays =>
            Props.aidCooldownDays > 0 ? Props.aidCooldownDays :
            (Props.cooldownDays > 0 ? Props.cooldownDays : 7);

        private bool PawnHasAnyRequiredRank(Pawn p)
        {
            // Liste aus XML (<requiredRankTraits>) hat Vorrang, sonst Single (<requiredRankTrait>)
            if (Props.requiredRankTraits != null && Props.requiredRankTraits.Count > 0)
                return Props.requiredRankTraits.Any(t => PawnHasTrait(p, t));
            return PawnHasTrait(p, Props.requiredRankTrait);
        }

        private static bool PawnHasTrait(Pawn p, string traitDefName)
        {
            if (string.IsNullOrEmpty(traitDefName)) return true;
            var td = DefDatabase<TraitDef>.GetNamedSilentFail(traitDefName);
            return td == null || (p.story?.traits?.HasTrait(td) ?? false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var map = parent.Map;
            if (map == null) yield break;

            var commsProgress = map.GetComponent<MapComponent_CommsProgress>();
            if (commsProgress == null) yield break;

            // 1) Vanilla Comms öffnen (Job)
            yield return new Command_Action
            {
                defaultLabel = "Open communications",
                defaultDesc = "Contact trade ships or factions.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CallInSupply", true),
                action = () => SelectPawnAndRun(p =>
                {
                    var job = JobMaker.MakeJob(JobDefOf.UseCommsConsole, parent);
                    p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                })
            };

            // 2) Starfleet Command (Quest Stub; sucht GiveQuest_* Incident)
            yield return new Command_Action
            {
                defaultLabel = "Contact Starfleet Command",
                defaultDesc  = "Open a channel to Admiral April to process missions.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/STCommand", true),
                action = () => SelectPawnAndRun(p =>
                {
                    var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, map);
                    var incident = DefDatabase<IncidentDef>.AllDefs
                        .FirstOrDefault(d => d.defName.StartsWith("GiveQuest_"));
                    if (incident == null)
                    {
                        Messages.Message("[YASTM] No quest incident (GiveQuest_*) found.", MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    incident.Worker.TryExecute(parms);
                    Messages.Message("Starfleet Command acknowledged.", MessageTypeDefOf.PositiveEvent);
                })
            };

            // 3) Request aid (Cooldown + Rangprüfung)
            var reqAid = new Command_Action
            {
                defaultLabel = "Request aid",
                defaultDesc = "Request emergency supplies or support (Commander+, cooldown).",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CallInSupply", true),
                action = () => SelectPawnAndRun(p =>
                {
                    if (!PawnHasAnyRequiredRank(p))
                    {
                        Messages.Message("Requires Commander rank or higher.", MessageTypeDefOf.RejectInput);
                        return;
                    }

                    if (!commsProgress.AidReadyNow)
                    {
                        var left = Mathf.Max(0, commsProgress.CooldownTicksLeft).ToStringTicksToPeriod();
                        Messages.Message($"Aid on cooldown: {left}.", MessageTypeDefOf.RejectInput);
                        return;
                    }

                    // Robuster Incident-Lookup (kein DefOf nötig)
                    IncidentDef incident =
                          DefDatabase<IncidentDef>.GetNamedSilentFail("ResourcePodCrash")
                       ?? DefDatabase<IncidentDef>.GetNamedSilentFail("ResourcePodCrashSingle")
                       ?? DefDatabase<IncidentDef>.GetNamedSilentFail("OrbitalTraderArrival")
                       ?? DefDatabase<IncidentDef>.AllDefs.FirstOrDefault(d => d.category == IncidentCategoryDefOf.Misc);

                    if (incident == null)
                    {
                        Messages.Message("[YASTM] No suitable incident found for aid.", MessageTypeDefOf.RejectInput);
                        return;
                    }

                    var parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
                    parms.points = 250f;
                    incident.Worker.TryExecute(parms);

                    commsProgress.nextAidAllowedTick = Find.TickManager.TicksGame + GenDate.TicksPerDay * CooldownDays;
                    Messages.Message("Starfleet aid incoming.", MessageTypeDefOf.PositiveEvent);
                })
            };

            if (!commsProgress.AidReadyNow)
            {
                reqAid.Disable($"Cooldown: {commsProgress.CooldownTicksLeft.ToStringTicksToPeriod()}");
            }

            yield return reqAid;
        }

        private void SelectPawnAndRun(System.Action<Pawn> run)
        {
            var map = parent.Map;
            var candidates = map.mapPawns.FreeColonists.Where(p =>
                !p.Downed && !p.Dead &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Talking) &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)).ToList();

            if (candidates.Count == 0)
            {
                Messages.Message("Select a healthy colonist on this map to operate the console.", MessageTypeDefOf.RejectInput);
                return;
            }

            var opts = new List<FloatMenuOption>();
            foreach (var p in candidates)
                opts.Add(new FloatMenuOption(p.LabelShortCap, () => run(p)));

            Find.WindowStack.Add(new FloatMenu(opts));
        }
    }
}
