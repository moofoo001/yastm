// Source/Comps/CompAlertPanelGizmo.cs
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class CompProperties_AlertPanelGizmo : CompProperties
    {
        // Sekunden (aus XML/Alias oder Defaults)
        public int redCooldownSeconds   = 5;
        public int yellowCooldownSeconds= 5;
        public int blinkSeconds         = 3;

        // Dauer in Ticks (0 = unendlich bis manuell OFF)
        public int redDurationTicks     = 0;
        public int yellowDurationTicks  = 0;

        public CompProperties_AlertPanelGizmo()
        {
            compClass = typeof(CompAlertPanelGizmo);
        }
    }

    public class CompAlertPanelGizmo : ThingComp
    {
        public CompProperties_AlertPanelGizmo Props => (CompProperties_AlertPanelGizmo)props;
        private MapComponent_AlertPanel MC => parent?.Map?.GetComponent<MapComponent_AlertPanel>();

        // ---------- RED ----------
        IEnumerable<Gizmo> RedGizmo()
        {
            if (parent?.Map == null) yield break;
            if (!(parent.Faction?.IsPlayer ?? false)) yield break;

            var mc = MC; if (mc == null) yield break;

            var cmd = new Command_Action
            {
                defaultLabel = "ST.AlertPanel.Red.Label".Translate(),
                defaultDesc  = "ST.AlertPanel.Red.Desc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RedAlert", true),
                hotKey       = KeyBindingDefOf.Misc3,
                action       = () =>
                {
                    // OFF ist immer erlaubt
                    if (mc.IsRedAlertOn)
                    {
                        mc.StopRedAlert();
                        return;
                    }

                    // ON nur, wenn kein Cooldown
                    if (mc.IsRedOnCooldown())
                    {
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                        Messages.Message("ST.AlertPanel.Cooldown".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    mc.StartRedAlert(Props.blinkSeconds, Props.redDurationTicks);
                    mc.ArmRedCooldownSeconds(Props.redCooldownSeconds);
                }
            };

            // Label/Timer
            bool showTimer = YASTM_Mod.Settings?.showAlertTimerInGizmo ?? true;
            int now = Find.TickManager.TicksGame;

            if (mc.BlinkUntilTickRed > now)
                cmd.defaultLabel += " !";

            if (mc.IsRedAlertOn)
            {
                cmd.defaultLabel += " (ON)";
                if (showTimer && mc.RedEndTick > now)
                {
                    int sec = Mathf.Max(0, (mc.RedEndTick - now) / 60);
                    cmd.defaultLabel += $" {sec/60:D2}:{sec%60:D2}";
                }
            }
            else
            {
                // Cooldown-Anzeige, wenn OFF
                if (showTimer && mc.IsRedOnCooldown())
                {
                    int rem = Mathf.Max(0, (mc.NextAllowedTickRed - now + 59) / 60);
                    cmd.defaultLabel += $" [CD {rem/60:D2}:{rem%60:D2}]";
                }
            }

            yield return cmd;
        }

        // ---------- YELLOW ----------
        IEnumerable<Gizmo> YellowGizmo()
        {
            if (parent?.Map == null) yield break;
            if (!(parent.Faction?.IsPlayer ?? false)) yield break;

            var mc = MC; if (mc == null) yield break;

            var cmd = new Command_Action
            {
                defaultLabel = "ST.AlertPanel.Yellow.Label".Translate(),
                defaultDesc  = "ST.AlertPanel.Yellow.Desc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/YellowAlert", true),
                hotKey       = KeyBindingDefOf.Misc4,
                action       = () =>
                {
                    if (mc.IsYellowAlertOn)
                    {
                        mc.StopYellowAlert();
                        return;
                    }

                    if (mc.IsYellowOnCooldown())
                    {
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                        Messages.Message("ST.AlertPanel.Cooldown".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    mc.StartYellowAlert(Props.blinkSeconds, Props.yellowDurationTicks);
                    mc.ArmYellowCooldownSeconds(Props.yellowCooldownSeconds);
                }
            };

            // Label/Timer
            bool showTimer = YASTM_Mod.Settings?.showAlertTimerInGizmo ?? true;
            int now = Find.TickManager.TicksGame;

            if (mc.BlinkUntilTickYellow > now)
                cmd.defaultLabel += " !";

            if (mc.IsYellowAlertOn)
            {
                cmd.defaultLabel += " (ON)";
                if (showTimer && mc.YellowEndTick > now)
                {
                    int sec = Mathf.Max(0, (mc.YellowEndTick - now) / 60);
                    cmd.defaultLabel += $" {sec/60:D2}:{sec%60:D2}";
                }
            }
            else
            {
                if (showTimer && mc.IsYellowOnCooldown())
                {
                    int rem = Mathf.Max(0, (mc.NextAllowedTickYellow - now + 59) / 60);
                    cmd.defaultLabel += $" [CD {rem/60:D2}:{rem%60:D2}]";
                }
            }

            yield return cmd;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in RedGizmo()) yield return g;
            foreach (var g in YellowGizmo()) yield return g;
        }
    }
}
