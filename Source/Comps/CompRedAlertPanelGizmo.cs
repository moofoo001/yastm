using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class CompProperties_RedAlertPanel : CompProperties
    {
        public int durationTicks = 18000; // ~5 Stunden
        public int cooldownTicks = 90000; // ~1.5 Tage

        public CompProperties_RedAlertPanel()
        {
            compClass = typeof(CompRedAlertPanel);
        }
    }

    public class CompRedAlertPanel : ThingComp
    {
        public CompProperties_RedAlertPanel Props => (CompProperties_RedAlertPanel)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer)
                yield break;

            var cmd = new Command_Action
            {
                defaultLabel = "Engage Red Alert",
                defaultDesc  = "Trigger a colony-wide Red Alert buff with a cooldown.",
                icon         = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", false),
                action       = Trigger
            };

            // Power-Check
            var power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                cmd.Disable("Needs power.");
                yield return cmd;
                yield break;
            }

            // Cooldown-Check
            var mc  = parent.Map?.GetComponent<MapComponent_RedAlert>();
            int now = Find.TickManager.TicksGame;
            if (mc != null && now < mc.NextAllowedTick)
            {
                int rem = mc.NextAllowedTick - now;
                cmd.Disable("Red Alert recharging: " + rem.ToStringTicksToPeriod());
            }

            yield return cmd;
        }

        void Trigger()
        {
            var map = parent.Map;
            if (map == null) return;

            // Power-Guard
            var power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                Messages.Message("Needs power.", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            var mc  = map.GetComponent<MapComponent_RedAlert>();
            int now = Find.TickManager.TicksGame;

            // Cooldown-Guard
            if (mc != null && now < mc.NextAllowedTick)
            {
                int rem = mc.NextAllowedTick - now;
                Messages.Message("Red Alert recharging: " + rem.ToStringTicksToPeriod(), parent, MessageTypeDefOf.RejectInput);
                return;
            }

            // Hediff anwenden
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ST_RedAlert");
            if (hediffDef == null)
            {
                Messages.Message("Missing hediff: ST_RedAlert", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                // refresh statt stacken
                var existing = p.health?.hediffSet?.GetFirstHediffOfDef(hediffDef);
                HediffWithComps h;
                if (existing is HediffWithComps hwc)
                {
                    h = hwc;
                }
                else
                {
                    h = p.health?.AddHediff(hediffDef) as HediffWithComps;
                }

                if (h != null)
                {
                    var disp = h.TryGetComp<HediffComp_Disappears>();
                    if (disp != null) disp.ticksToDisappear = Props.durationTicks;
                }

                // kleines Feedback
                MoteMaker.ThrowText(p.DrawPos, map, "RED ALERT", Color.red, 1.4f);
            }

            // Blink: sofortiger Flash + kurzes Nachblinken (MapComponent tickt)
            var fleck = DefDatabase<FleckDef>.GetNamedSilentFail("ST_RedAlertBlink");
            if (fleck == null || fleck.graphicData == null || fleck.graphicData.texPath.NullOrEmpty())
                fleck = FleckDefOf.Smoke; // Fallback

            FleckMaker.AttachedOverlay(parent, fleck, Vector3.zero, 1.2f);
            if (mc != null) mc.BlinkUntilTick = now + 360; // ~6 Sekunden

            // Sound: kurzer Ping + (optional) loopende Sirene für die Blinkdauer
            var ping = DefDatabase<SoundDef>.GetNamedSilentFail("ST_RedAlert_SirenPing");
            if (ping != null) ping.PlayOneShot(SoundInfo.OnCamera());

            var longDef = DefDatabase<SoundDef>.GetNamedSilentFail("ST_RedAlert_SirenLong");
            if (longDef != null && mc != null)
            {
                mc.activeSiren?.End();
                mc.activeSiren = SoundStarter.TrySpawnSustainer(longDef, SoundInfo.OnCamera());
                mc.activeSiren?.Maintain();
            }

            // Cooldown setzen
            if (mc != null) mc.NextAllowedTick = now + Props.cooldownTicks;

            Messages.Message("Red Alert engaged.", parent, MessageTypeDefOf.PositiveEvent);
        }
    }
}
