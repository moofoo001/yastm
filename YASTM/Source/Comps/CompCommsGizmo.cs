using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using YASTM.MapSystems;
/// using StarTrekFactions;

namespace YASTM.Comps
{
    public class CompProperties_CommsGizmo : CompProperties
    {
        public bool enableContactStarfleet = true;
        public bool enableRequestAid = true;
        public bool enableTransmitScanData = true;

        public List<TraitDef> requiredOfficerTraits = new List<TraitDef>();

        public float contactCooldownDays = 5f;
        public float aidCooldownDays = 8f;

        public int maxActiveQuests = 2;

        /// <summary>
        /// Optional explicit quest pool for Starfleet-style assignments.
        /// If empty, a suitable fallback will be chosen.
        /// </summary>
        public List<QuestScriptDef> questPool = new List<QuestScriptDef>();

        /// <summary>
        /// Chance that a Starfleet quest (from questPool) will be chosen
        /// instead of a generic vanilla root quest.
        /// </summary>
        public float starfleetQuestChance = 0.4f;

        public CompProperties_CommsGizmo()
        {
            compClass = typeof(CompCommsGizmo);
        }
    }

    public class CompCommsGizmo : ThingComp
    {
        private CompProperties_CommsGizmo Props => (CompProperties_CommsGizmo)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Map map = parent.Map;
            var progress = map?.GetComponent<MapComponent_CommsProgress>();
            if (progress == null)
                yield break;

            bool PowerOk()
            {
                var pwr = parent.TryGetComp<CompPowerTrader>();
                return pwr == null || pwr.PowerOn;
            }

            bool BeaconOk(out string reason)
            {
                reason = null;
                var link = parent.GetComp<CompConsoleLink>();
                if (link != null && !link.HasLink(out reason))
                    return false;
                return true;
            }

            bool IsCommissioned(Pawn p)
            {
                if (p == null || p.Dead || p.Downed) return false;
                if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Talking)) return false;

                if (Props.requiredOfficerTraits == null || Props.requiredOfficerTraits.Count == 0)
                    return true;

                return Props.requiredOfficerTraits.Any(td => p.story?.traits?.HasTrait(td) == true);
            }

            IEnumerable<Pawn> ValidOperators()
            {
                foreach (var p in map.mapPawns.FreeColonistsSpawned)
                    if (IsCommissioned(p))
                        yield return p;
            }

            void ChooseOperatorAndRun(string failMsg, Action<Pawn> withPawn)
            {
                var pawns = ValidOperators().ToList();
                if (pawns.Count == 0)
                {
                    Messages.Message(failMsg, MessageTypeDefOf.RejectInput);
                    return;
                }
                if (pawns.Count == 1)
                {
                    withPawn(pawns[0]);
                    return;
                }
                var opts = pawns.Select(p => new FloatMenuOption(p.LabelShortCap, () => withPawn(p))).ToList();
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            int CountAllActiveOrOfferedQuests()
            {
                return Find.QuestManager.QuestsListForReading.Count(q =>
                    q.State == QuestState.Ongoing || q.State == QuestState.NotYetAccepted);
            }

            // ---------- CONTACT STARFLEET ----------
            if (Props.enableContactStarfleet)
            {
                var cmd = new Command_Action
                {
                    defaultLabel = "Contact Starfleet Command",
                    defaultDesc  = "Open a channel to Starfleet and request an assignment.",
                    icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/STCommand", true),
                    action = () =>
                    {
                        if (!PowerOk())
                        {
                            Messages.Message("The console is unpowered.", MessageTypeDefOf.RejectInput);
                            return;
                        }
                        if (!BeaconOk(out string linkReason))
                        {
                            Messages.Message(linkReason ?? "A Starfleet comm beacon must be linked.", MessageTypeDefOf.RejectInput);
                            return;
                        }

                        int active = CountAllActiveOrOfferedQuests();
                        if (active >= Props.maxActiveQuests)
                        {
                            Messages.Message($"HQ is busy. Active quests: {active}/{Props.maxActiveQuests}.", MessageTypeDefOf.RejectInput);
                            return;
                        }

                        ChooseOperatorAndRun("A commissioned Starfleet officer must operate the console.", pawn =>
                        {
                            var watcher = StarTrekFactions.GameComponent_QuestWatchers.Instance;

                            // Build Starfleet pool from explicit questPool
                            var starfleetPool = (Props.questPool ?? new List<QuestScriptDef>())
                                .Where(q => q != null)
                                .Distinct()
                                .ToList();

                            // If Obelisk assignment was already used, remove all Obelisk quests from the Starfleet pool
                            if (watcher != null && watcher.ObeliskAssignmentUsed)
                            {
                                starfleetPool = starfleetPool
                                    .Where(q => q.defName == null || !q.defName.Contains("Obelisks"))
                                    .ToList();
                            }

                            // Build vanilla pool from all root-capable quest scripts not in the Starfleet pool
                            var vanillaPool = DefDatabase<QuestScriptDef>.AllDefs
                                .Where(q =>
                                    !q.isRootSpecial &&
                                    q.rootSelectionWeight > 0f &&
                                    !starfleetPool.Contains(q))
                                .ToList();

                            if (starfleetPool.Count == 0 && vanillaPool.Count == 0)
                            {
                                Messages.Message("No suitable assignments available.", MessageTypeDefOf.RejectInput);
                                return;
                            }

                            QuestScriptDef chosen = null;

                            bool useStarfleet =
                                starfleetPool.Count > 0 &&
                                Rand.Chance(Props.starfleetQuestChance <= 0f || Props.starfleetQuestChance >= 1f
                                    ? 0.4f
                                    : Props.starfleetQuestChance);

                            if (useStarfleet)
                            {
                                if (starfleetPool.Count > 0)
                                {
                                    chosen = starfleetPool.RandomElement();
                                }
                            }
                            else
                            {
                                if (vanillaPool.Count > 0)
                                {
                                    chosen = vanillaPool.RandomElementByWeight(q => q.rootSelectionWeight);
                                }
                                else if (starfleetPool.Count > 0)
                                {
                                    // fallback if vanilla is empty
                                    chosen = starfleetPool.RandomElement();
                                }
                            }

                            // Final fallback: if still nothing chosen, try the old hardcoded STQ quest
                            if (chosen == null)
                            {
                                chosen = DefDatabase<QuestScriptDef>.GetNamedSilentFail("STQ_Obelisks_I");
                            }

                            if (chosen == null)
                            {
                                Messages.Message("No suitable Starfleet assignment available.", MessageTypeDefOf.RejectInput);
                                return;
                            }

                            var slate = new Slate();
                            var quest = QuestUtility.GenerateQuestAndMakeAvailable(chosen, slate);

                            if (quest != null)
                            {
                                // Mark Obelisk assignment as used if we just picked an Obelisk quest
                                if (watcher != null && chosen.defName != null && chosen.defName.Contains("Obelisks"))
                                {
                                    watcher.ObeliskAssignmentUsed = true;
                                }

                                Messages.Message("Starfleet has transmitted an assignment.", MessageTypeDefOf.NeutralEvent);
                                progress.ArmContactCooldown(Props.contactCooldownDays);
                            }
                            else
                            {
                                Messages.Message("No response from Starfleet Command.", MessageTypeDefOf.RejectInput);
                            }
                        });
                    }
                };

                if (!progress.ContactReady)
                    cmd.Disable(progress.ContactCooldownLabel(map));
                yield return cmd;
            }

            // ---------- REQUEST AID ----------
            if (Props.enableRequestAid)
            {
                var cmd = new Command_Action
                {
                    defaultLabel = "Request Starfleet Aid",
                    defaultDesc  = "Request emergency rations and medicine.",
                    icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/CallInSupply", true),
                    action = () =>
                    {
                        if (!PowerOk())
                        {
                            Messages.Message("The console is unpowered.", MessageTypeDefOf.RejectInput);
                            return;
                        }
                        if (!BeaconOk(out string linkReason))
                        {
                            Messages.Message(linkReason ?? "A Starfleet comm beacon must be linked.", MessageTypeDefOf.RejectInput);
                            return;
                        }

                        ChooseOperatorAndRun("A commissioned Starfleet officer must operate the console.", pawn =>
                        {
                            // resolve drop spot
                            IntVec3 dropSpot = DropCellFinder.RandomDropSpot(map);

                            // resolve defs across different modlists
                            ThingDef mealDef =
                                DefDatabase<ThingDef>.GetNamedSilentFail("PackagedSurvivalMeal")
                                ?? DefDatabase<ThingDef>.GetNamedSilentFail("PackageSurvivalMeal")
                                ?? DefDatabase<ThingDef>.GetNamedSilentFail("MealSurvivalPack")
                                ?? DefDatabase<ThingDef>.GetNamedSilentFail("MealSimple")
                                ?? DefDatabase<ThingDef>.GetNamedSilentFail("SimpleMeal");

                            ThingDef medDef =
                                DefDatabase<ThingDef>.GetNamedSilentFail("MedicineIndustrial")
                                ?? DefDatabase<ThingDef>.GetNamedSilentFail("Medicine");

                            if (mealDef == null || medDef == null)
                            {
                                Messages.Message("Aid failed: could not resolve supply defs.", MessageTypeDefOf.RejectInput);
                                return;
                            }

                            // create actual Thing instances
                            var meal = ThingMaker.MakeThing(mealDef);
                            meal.stackCount = 24;

                            var meds = ThingMaker.MakeThing(medDef);
                            meds.stackCount = 12;

                            var things = new List<Thing> { meal, meds };

                            DropPodUtility.DropThingsNear(dropSpot, map, things, 110,
                                canInstaDropDuringInit: false, leaveSlag: false);

                            Messages.Message("Starfleet aid has arrived in a drop pod.", MessageTypeDefOf.PositiveEvent);
                            progress.ArmAidCooldown(Props.aidCooldownDays);
                        });
                    }
                };

                if (!progress.AidReady)
                    cmd.Disable(progress.AidCooldownLabel(map));
                yield return cmd;
            }

            // ---------- TRANSMIT SCAN DATA ----------
            if (Props.enableTransmitScanData)
            {
                var cmd = new Command_Action
                {
                    defaultLabel = "Transmit Scan Data",
                    defaultDesc  = "Send combined obelisk telemetry to Starfleet.",
                    icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ScienceScan", true),
                    action = () =>
                    {
                        if (!PowerOk())
                        {
                            Messages.Message("The console is unpowered.", MessageTypeDefOf.RejectInput);
                            return;
                        }
                        if (!BeaconOk(out string linkReason))
                        {
                            Messages.Message(linkReason ?? "A Starfleet comm beacon must be linked.", MessageTypeDefOf.RejectInput);
                            return;
                        }

                        var flow = map.GetComponent<MapComponent_ObeliskFlow>();
                        if (flow == null || !flow.CanTransmit())
                        {
                            Messages.Message("No complete scan data available.", MessageTypeDefOf.RejectInput);
                            return;
                        }

                        ChooseOperatorAndRun("A commissioned Starfleet officer must operate the console.", pawn =>
                        {
                            flow.OnTransmit();
                            Messages.Message("Telemetry uplink complete.", MessageTypeDefOf.PositiveEvent);
                        });
                    }
                };

                var flowForLabel = map.GetComponent<MapComponent_ObeliskFlow>();
                if (flowForLabel == null || !flowForLabel.CanTransmit())
                    cmd.Disable("Both obelisks must be scanned before you can transmit.");
                yield return cmd;
            }
        }
    }
}
