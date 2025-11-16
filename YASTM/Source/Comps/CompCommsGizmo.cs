using System.Collections.Generic;
using System.Linq;
using UnityEngine;                 // Texture2D
using Verse;
using RimWorld;
using RimWorld.QuestGen;          // Slate, QuestUtility

namespace YASTM
{
    public class CompCommsGizmo : ThingComp
    {
        private const int MaxActiveQuestsFromButton = 2;   // hard limit
        private const int ContactCooldownTicks = 30000;    // ~0.5 day; tune as you like
        private const int AidCooldownTicks = 600000;       // 10 days; you used 5–10d earlier

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!parent.Spawned || parent.Map == null) yield break;

            yield return MakeContactStarfleetGizmo();
            yield return MakeRequestAidGizmo();
        }

        // -------------------------------
        // Contact Starfleet Command
        // -------------------------------
        private Command_Action MakeContactStarfleetGizmo()
        {
            var map = parent.Map;
            var mc = map.GetComponent<MapComponent_CommsProgress>() ?? new MapComponent_CommsProgress(map);
            int activeQuests = MapComponent_CommsProgress.CountActiveQuestsAllSources();

            int now = Find.TickManager.TicksGame;
            int cdRemain = mc.NextContactAllowedTick > now ? (mc.NextContactAllowedTick - now) : 0;

            var cmd = new Command_Action
            {
                defaultLabel = cdRemain > 0
                    ? $"Contact Starfleet Command ({cdRemain.ToStringTicksToPeriod()}) [{Mathf.Min(activeQuests, MaxActiveQuestsFromButton)}/{MaxActiveQuestsFromButton}]"
                    : $"Contact Starfleet Command [{Mathf.Min(activeQuests, MaxActiveQuestsFromButton)}/{MaxActiveQuestsFromButton}]",
                defaultDesc = "Open a channel to Starfleet Command and request a mission.",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/STCommand", true),
                action = () =>
                {
                    int nowInner = Find.TickManager.TicksGame;

                    // global limit: any active quests count
                    int active = MapComponent_CommsProgress.CountActiveQuestsAllSources();
                    if (active >= MaxActiveQuestsFromButton)
                    {
                        Messages.Message("Too many active quests. Complete some before contacting Starfleet again.",
                            MessageTypeDefOf.RejectInput);
                        return;
                    }

                    // local contact cooldown
                    if (mc.NextContactAllowedTick > nowInner)
                    {
                        Messages.Message($"You must wait { (mc.NextContactAllowedTick - nowInner).ToStringTicksToPeriod() } before contacting Starfleet again.",
                            MessageTypeDefOf.RejectInput);
                        return;
                    }

                    Quest q = TryStartStarfleetQuest(map);
                    if (q != null)
                    {
                        mc.RegisterStarfleetQuest(q);
                        mc.NextContactAllowedTick = nowInner + ContactCooldownTicks;
                        Messages.Message("A new mission from Starfleet Command is available.",
                            MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        mc.NextContactAllowedTick = nowInner + (ContactCooldownTicks / 2); // short fail cooldown
                        Messages.Message("No response from Starfleet Command.",
                            MessageTypeDefOf.NeutralEvent);
                    }
                }
            };

            if (activeQuests >= MaxActiveQuestsFromButton || cdRemain > 0)
                cmd.Disable(cdRemain > 0
                    ? $"Cooling down for {cdRemain.ToStringTicksToPeriod()}."
                    : $"Too many active quests (max {MaxActiveQuestsFromButton}).");

            return cmd;
        }

        /// <summary>
        /// Tries to start any available Starfleet quest script (your STQ_* scripts).
        /// Adjust the pick logic if you need a specific chain first.
        /// </summary>
        private static Quest TryStartStarfleetQuest(Map map)
        {
            // Prefer your "STQ_" scripts
            var all = DefDatabase<QuestScriptDef>.AllDefsListForReading;
            var candidates = all.Where(d => d.defName.StartsWith("STQ_")).ToList();
            if (candidates.Count == 0) return null;

            // Simple pick: first not currently ongoing by tag (tweak to your liking)
            QuestScriptDef script = candidates.RandomElement();

            var slate = new Slate();
            slate.Set("map", map);

            Quest q = QuestUtility.GenerateQuestAndMakeAvailable(script, slate);
            if (q != null) QuestUtility.SendLetterQuestAvailable(q);
            return q;
        }

        // -------------------------------
        // Request Aid
        // -------------------------------
        private Command_Action MakeRequestAidGizmo()
        {
            var map = parent.Map;
            var mc = map.GetComponent<MapComponent_CommsProgress>() ?? new MapComponent_CommsProgress(map);

            int now = Find.TickManager.TicksGame;
            int cdRemain = mc.NextAidAllowedTick > now ? (mc.NextAidAllowedTick - now) : 0;

            var cmd = new Command_Action
            {
                defaultLabel = cdRemain > 0
                    ? $"Request aid ({cdRemain.ToStringTicksToPeriod()})"
                    : "Request aid",
                defaultDesc = "Request a single aid package from Starfleet (cooldown applies).",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CallInSupply", true),
                action = () =>
                {
                    int nowInner = Find.TickManager.TicksGame;

                    if (mc.NextAidAllowedTick > nowInner)
                    {
                        Messages.Message($"Aid channel is cooling down for { (mc.NextAidAllowedTick - nowInner).ToStringTicksToPeriod() }.",
                            MessageTypeDefOf.RejectInput);
                        return;
                    }

                    // Your existing aid logic (pods/goodwill/etc.) can go here; we keep it simple and safe:
                    // A small goodwill bump with a random non-hostile faction.
                    var fac = Find.FactionManager.AllFactionsVisible
                        .Where(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer))
                        .RandomElementWithFallback();

                    if (fac != null) fac.TryAffectGoodwillWith(Faction.OfPlayer, 5);

                    mc.NextAidAllowedTick = nowInner + AidCooldownTicks;
                    Messages.Message("Starfleet has acknowledged your request. Aid is being processed.",
                        MessageTypeDefOf.PositiveEvent);
                }
            };

            if (cdRemain > 0)
                cmd.Disable($"Cooling down for {cdRemain.ToStringTicksToPeriod()}.");

            return cmd;
        }
    }
}
