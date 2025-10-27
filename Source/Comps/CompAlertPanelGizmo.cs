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
        // kleine, interne Defaults – können später in Mod-Settings wandern
        public int redCooldownSeconds = 5;
        public int yellowCooldownSeconds = 5;
        public int blinkSeconds = 3;

        public CompProperties_AlertPanelGizmo()
        {
            compClass = typeof(CompAlertPanelGizmo);
        }
    }

    public class CompAlertPanelGizmo : ThingComp
    {
        public CompProperties_AlertPanelGizmo Props => (CompProperties_AlertPanelGizmo)props;

        IEnumerable<Gizmo> RedGizmo()
        {
            if (parent?.Map == null || parent.Faction != Faction.OfPlayer) yield break;

            var map = parent.Map;
            var mc = map.GetComponent<YASTM.MapComponent_AlertPanel>();
            if (mc == null) yield break;

            int now = Find.TickManager.TicksGame;
            int cdTicks = Props.redCooldownSeconds * 60;
            int blinkTicks = Props.blinkSeconds * 60;

            var cmd = new Command_Action
            {
                defaultLabel = "ST.AlertPanel.Red.Label".Translate(),   // z.B. "Red Alert"
                defaultDesc  = "ST.AlertPanel.Red.Desc".Translate(),    // Erklärungstext
                icon = null, // Optional: ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RedAlert", true)
                action = () =>
                {
                    // MapComponent referenzieren
                    var map2 = parent.Map;
                    var mc2 = map2?.GetComponent<YASTM.MapComponent_AlertPanel>();
                    if (mc2 == null) return;

                    // Red toggeln, Yellow aus
                    bool newState = !mc2.IsRedAlertOn;
                    mc2.IsRedAlertOn = newState;
                    mc2.IsYellowAlertOn = false;

                    // Blinken
                    mc2.BlinkUntilTickRed = now + blinkTicks;

                    // Cooldown setzen
                    mc2.NextAllowedTickRed = now + cdTicks;

                    // Sirene starten (MapComponent-Methode nutzen, falls vorhanden)
                    var redSnd = DefDatabase<SoundDef>.GetNamed("ST_RedAlert_SirenLong", false);
                    if (newState && redSnd != null)
                    {
                        // bevorzugt Start-Methode (setzt auch Sustainer im MC)
                        var m = typeof(YASTM.MapComponent_AlertPanel).GetMethod("StartRedSiren");
                        if (m != null)
                        {
                            m.Invoke(mc2, new object[] { redSnd });
                        }
                        else
                        {
                            // Fallback: direkt spawnen mit Settings-Lautstärke
                            var info = SoundInfo.OnCamera(MaintenanceType.None);
                            info.volumeFactor = Mathf.Clamp01(YASTM_Mod.Settings?.AlertVolume01 ?? 1f);
                            var sust = SoundStarter.TrySpawnSustainer(redSnd, info);
                            // Wenn dein MC public Felder 'activeSiren' hat, kannst du sie hier setzen:
                            // mc2.activeSiren = sust;
                        }
                    }

                    // Wenn Red aus → Yellow-Sirene ggf. beenden übernimmt dein MC-Tick
                }
            };

            // Cooldown-Disable
            if (now < mc.NextAllowedTickRed)
            {
                int sec = Mathf.CeilToInt((mc.NextAllowedTickRed - now) / 60f);
                cmd.Disable("ST.AlertPanel.Cooldown".Translate(sec));
            }

            // Optionales Status-Suffix
            if (mc.IsRedAlertOn)
                cmd.defaultLabel += " (ON)";

            yield return cmd;
        }

        IEnumerable<Gizmo> YellowGizmo()
        {
            if (parent?.Map == null || parent.Faction != Faction.OfPlayer) yield break;

            var map = parent.Map;
            var mc = map.GetComponent<YASTM.MapComponent_AlertPanel>();
            if (mc == null) yield break;

            int now = Find.TickManager.TicksGame;
            int cdTicks = Props.yellowCooldownSeconds * 60;
            int blinkTicks = Props.blinkSeconds * 60;

            var cmd = new Command_Action
            {
                defaultLabel = "ST.AlertPanel.Yellow.Label".Translate(), // z.B. "Yellow Alert"
                defaultDesc  = "ST.AlertPanel.Yellow.Desc".Translate(),
                icon = null, // Optional: ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/YellowAlert", true)
                action = () =>
                {
                    var map2 = parent.Map;
                    var mc2 = map2?.GetComponent<YASTM.MapComponent_AlertPanel>();
                    if (mc2 == null) return;

                    bool newState = !mc2.IsYellowAlertOn;
                    mc2.IsYellowAlertOn = newState;
                    mc2.IsRedAlertOn = false;

                    mc2.BlinkUntilTickYellow = now + blinkTicks;
                    mc2.NextAllowedTickYellow = now + cdTicks;

                    var yelSnd = DefDatabase<SoundDef>.GetNamed("ST_YellowAlert_Signal", false);
                    if (newState && yelSnd != null)
                    {
                        var m = typeof(YASTM.MapComponent_AlertPanel).GetMethod("StartYellowSiren");
                        if (m != null)
                        {
                            m.Invoke(mc2, new object[] { yelSnd });
                        }
                        else
                        {
                            var info = SoundInfo.OnCamera(MaintenanceType.None);
                            info.volumeFactor = Mathf.Clamp01(YASTM_Mod.Settings?.AlertVolume01 ?? 1f);
                            var sust = SoundStarter.TrySpawnSustainer(yelSnd, info);
                            // mc2.activeSirenYellow = sust; // falls Feld vorhanden & public
                        }
                    }
                }
            };

            if (now < mc.NextAllowedTickYellow)
            {
                int sec = Mathf.CeilToInt((mc.NextAllowedTickYellow - now) / 60f);
                cmd.Disable("ST.AlertPanel.Cooldown".Translate(sec));
            }

            if (mc.IsYellowAlertOn)
                cmd.defaultLabel += " (ON)";

            yield return cmd;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in RedGizmo()) yield return g;
            foreach (var g in YellowGizmo()) yield return g;
        }
    }
}
