using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using YASTM.MapSystems;

namespace YASTM.Comps
{
    public class CompProperties_CommsGizmo : CompProperties
    {
        public bool enableContactStarfleet = true;
        public bool enableRequestAid = true;
        public bool enableTransmitScanData = true;

        // Optional: which traits qualify a pawn as "commissioned officer"
        public List<TraitDef> requiredOfficerTraits = new List<TraitDef>();

        public float contactCooldownDays = 5f;
        public float aidCooldownDays = 8f;
        public int maxActiveQuests = 2;

        // Pool of quest scripts to pick from (falls back to STQ_Obelisks_I)
        public List<QuestScriptDef> questPool = new List<QuestScriptDef>();

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

            // ---------- helpers ----------
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

            IEnumerable<Pawn> ValidOperators()
            {
                // RimWorld-style: spawned, conscious, can talk, (optional) required traits
                foreach (var p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (p.Dead || p.Downed) continue;
                    if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Talking)) continue;

                    if (Props.requiredOfficerTraits != null && Props.requiredOfficerTraits.Count > 0)
                    {
                        bool hasAny = Props.requiredOfficerTraits.Any(td => p.story?.traits?.HasTrait(td) == true);
                        if (!hasAny) continue;
                    }
                    yield return p;
                }
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
                // Count *any* active/offered quests to throttle Starfleet contact
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

                        ChooseOperatorAndRun("No commissioned operator available.", pawn =>
                        {
                            // pick quest from pool (fallback to STQ_Obelisks_I)
                            var pool = Props.questPool?.Where(q => q != null).ToList() ?? new List<QuestScriptDef>();
                            var chosen = pool.Any()
                                ? pool.RandomElement()
                                : DefDatabase<QuestScriptDef>.GetNamedSilentFail("STQ_Obelisks_I");

                            if (chosen == null)
                            {
                                Messages.Message("No suitable Starfleet assignment available.", MessageTypeDefOf.RejectInput);
                                return;
                            }

                            var slate = new Slate();
                            var quest = QuestUtility.GenerateQuestAndMakeAvailable(chosen, slate);
                            if (quest != null)
                            {
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

                        ChooseOperatorAndRun("No commissioned operator available.", pawn =>
                        {
                            IntVec3 dropSpot = DropCellFinder.RandomDropSpot(map);

                            // robust def resolution across RW versions/modlists
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

                            var things = new List<Thing>();
                            var meal = ThingMaker.MakeThing(mealDef);
                            meal.stackCount = 24; things.Add(meal);
                            var med = ThingMaker.MakeThing(medDef);
                            med.stackCount = 12; things.Add(med);

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

                        ChooseOperatorAndRun("No commissioned operator available.", pawn =>
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
