using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace StarTrekFactions.Comps
{
    /// <summary>
    /// Comp for the Science Console: adds a gizmo to initiate the STQ subspace scan.
    /// It sends quest signals so the STQ quest can react (e.g. advance the “scan obelisk” stage).
    /// </summary>
    public class CompProperties_ScienceConsoleSubspaceScan : CompProperties
    {
        /// <summary>
        /// Quest signal fired when the scan is started.
        /// Hook this up as inSignal in your STQ quest.
        /// </summary>
        public string questSignalOnScanStarted = "ST_SubspaceScanStarted";

        /// <summary>
        /// Quest signal fired when the scan is completed.
        /// Hook this up as inSignal in your STQ quest.
        /// </summary>
        public string questSignalOnScanCompleted = "ST_SubspaceScanCompleted";

        /// <summary>
        /// Optional: research that must be finished before the gizmo appears.
        /// Leave null to always allow.
        /// </summary>
        public ResearchProjectDef requiredResearch;

        public CompProperties_ScienceConsoleSubspaceScan()
        {
            compClass = typeof(CompScienceConsoleSubspaceScan);
        }
    }

    public class CompScienceConsoleSubspaceScan : ThingComp
    {
        /// <summary>
        /// Baseline scan duration (with 1 active scanner) in ticks (~2 in-game hours).
        /// </summary>
        private const int ScanDurationTicks = 6000;

        /// <summary>
        /// Speed bonus per extra active scanner.
        /// Example: 1 scanner = 1.0x, 2 scanners = 1.5x, 3 scanners = 2.0x, etc.
        /// </summary>
        private const float ExtraScannerSpeedBonus = 0.5f;

        private bool scanInProgress;
        private int  scanTicksLeft;

        public CompProperties_ScienceConsoleSubspaceScan Props
            => (CompProperties_ScienceConsoleSubspaceScan)props;

        /// <summary>
        /// 0..1 progress value for UI.
        /// </summary>
        public float ScanProgress01 =>
            scanInProgress
                ? Mathf.Clamp01(1f - (float)scanTicksLeft / ScanDurationTicks)
                : 0f;

        /// <summary>
        /// Counts active (powered) subspace scanners on this map.
        /// </summary>
        public int ActiveScannerCount => CountActiveScanners();

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Only for player-owned, powered consoles.
            if (parent.Faction != Faction.OfPlayer)
                yield break;

            var power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
                yield break;

            // Optional research gate.
            if (Props.requiredResearch != null && !Props.requiredResearch.IsFinished)
                yield break;

            // While a scan is running, show a progress gizmo instead of the start button.
            if (scanInProgress)
            {
                yield return new Gizmo_ScienceScanProgress
                {
                    comp = this
                };
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "Initiate subspace scan",
                defaultDesc =
                    "Use the science console to coordinate a subspace scan of the obelisk network.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ST_InitiateScan", true),
                action = StartScanAction
            };
        }

        private void StartScanAction()
        {
            if (scanInProgress)
                return;

            // Require at least one active subspace scanner.
            int scanners = CountActiveScanners();
            if (scanners <= 0)
            {
                Messages.Message(
                    "No powered subspace scanners are available on this map.",
                    parent,
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            scanInProgress = true;
            scanTicksLeft  = ScanDurationTicks;

            // Fire “started” signal for the STQ quest (if any listens).
            SendQuestSignal(Props.questSignalOnScanStarted);

            // Small feedback with scanner count.
            string info = scanners == 1
                ? "Subspace scan initiated from the science console. 1 scanner online."
                : $"Subspace scan initiated from the science console. {scanners} scanners online.";
            Messages.Message(info, parent, MessageTypeDefOf.TaskCompletion, false);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!scanInProgress)
                return;

            // Compute speed factor based on active scanners.
            int scanners = CountActiveScanners();
            float speedFactor = 1f + Mathf.Max(0, scanners - 1) * ExtraScannerSpeedBonus;
            if (speedFactor < 1f)
                speedFactor = 1f;

            // How many baseline ticks do we consume this game tick?
            int delta = Mathf.Max(1, Mathf.RoundToInt(speedFactor));
            scanTicksLeft -= delta;

            if (scanTicksLeft > 0)
                return;

            scanInProgress = false;
            scanTicksLeft  = 0;

            // Fire “completed” signal.
            SendQuestSignal(Props.questSignalOnScanCompleted);

            QuestUtility.SendQuestTargetSignals(
                null,                      // quest (null = broadcast)
                "STQ.Obelisks.ScanComplete",
                parent                     // lookTargets
            );

            Messages.Message(
                "Subspace scan completed.",
                parent,
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        /// <summary>
        /// Counts all powered subspace scanners (things with YASTM.CompProperties_SubspaceScanner)
        /// on the console's map.
        /// </summary>
        private int CountActiveScanners()
        {
            var map = parent.Map;
            if (map == null)
                return 0;

            int count = 0;
                var things = map.listerThings.AllThings;
                for (int i = 0; i < things.Count; i++)
                {
                    var t = things[i];
                    if (t == null || !t.Spawned)
                        continue;

                    // only Things that actually have comps
                    if (t is not ThingWithComps twc)
                        continue;

                    bool isScanner = false;
                    var comps = twc.AllComps;
                    if (comps != null)
                    {
                        for (int j = 0; j < comps.Count; j++)
                        {
                            var c = comps[j];
                            if (c?.props is global::YASTM.Comps.CompProperties_SubspaceScanner)
                            {
                                isScanner = true;
                                break;
                            }
                        }
                    }

                    if (!isScanner)
                        continue;

                    var power = t.TryGetComp<CompPowerTrader>();
                    if (power != null && !power.PowerOn)
                        continue;

                    count++;
                }

            return count;
        }

        private void SendQuestSignal(string signalTag)
        {
            if (signalTag.NullOrEmpty())
                return;

            var signal = new Signal(signalTag, parent.Named("SUBJECT"));
            Find.SignalManager.SendSignal(signal);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref scanInProgress, "ST_scanInProgress");
            Scribe_Values.Look(ref scanTicksLeft,  "ST_scanTicksLeft", 0);
        }
    }

    /// <summary>
    /// Simple progress gizmo for the science console scan.
    /// Shows a bar and the number of active scanners.
    /// </summary>
    public class Gizmo_ScienceScanProgress : Gizmo
    {
        public CompScienceConsoleSubspaceScan comp;

        private const float GizmoWidth = 212f;
        private const float GizmoHeight = 75f;

        public override float GetWidth(float maxWidth)
        {
            return GizmoWidth;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);
            Widgets.DrawWindowBackground(rect);

            if (comp == null)
            {
                return new GizmoResult(GizmoState.Clear);
            }

            Rect labelRect = rect.TopPart(0.4f).ContractedBy(6f);
            Rect barRect = rect.BottomPart(0.55f).ContractedBy(6f);

            // Label
            Text.Anchor = TextAnchor.MiddleLeft;
            string label = "Subspace scan in progress";
            int scanners = comp.ActiveScannerCount;
            if (scanners > 0)
            {
                label += $" ({scanners} scanner" + (scanners == 1 ? "" : "s") + ")";
            }
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            // Progress bar
            float fillPercent = comp.ScanProgress01;
            Widgets.FillableBar(barRect, fillPercent);

            if (Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(
                    rect,
                    "The science console is coordinating a subspace scan of the obelisk network. " +
                    "Additional powered scanners accelerate the process.");
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }
}
