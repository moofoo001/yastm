using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class CompProperties_AlertPanel : CompProperties
    {
        public int redDurationTicks = 18000;       // ~5h
        public int redCooldownTicks = 90000;       // ~1.5 Tage
        public int yellowDurationTicks = 9000;     // ~2.5h
        public int yellowCooldownTicks = 45000;    // ~0.75 Tage
        public bool playRedSirenLoop = true;       // Sustainer beim Rotalarm

        public CompProperties_AlertPanel()
        {
            compClass = typeof(CompAlertPanel);
        }
    }

    public class CompAlertPanel : ThingComp
    {
        public CompProperties_AlertPanel Props => (CompProperties_AlertPanel)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;

            var power = parent.GetComp<CompPowerTrader>();
            bool powered = power == null || power.PowerOn;
            int now = Find.TickManager.TicksGame;
            var mc = parent.Map?.GetComponent<MapComponent_AlertPanel>();

            // Yellow Alert Button
            var cmdYellow = new Command_Action
            {
                defaultLabel = "ST.YellowAlert.EngageButton".Translate(),
                defaultDesc  = "ST.YellowAlert.ButtonDesc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/YellowAlert", false),
                action       = TriggerYellow
            };
            if (!powered)
            {
                cmdYellow.Disable("ST.YellowAlert.NeedsPower".Translate());
            }
            else if (mc != null && now < mc.NextAllowedTickYellow)
            {
                int rem = mc.NextAllowedTickYellow - now;
                cmdYellow.Disable("ST.YellowAlert.Recharging".Translate(rem.ToStringTicksToPeriod()));
            }
            yield return cmdYellow;

            // Red Alert Button
            var cmdRed = new Command_Action
            {
                defaultLabel = "ST.RedAlert.EngageButton".Translate(),
                defaultDesc  = "ST.RedAlert.ButtonDesc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/RedAlert", false),
                action       = TriggerRed
            };
            if (!powered)
            {
                cmdRed.Disable("ST.RedAlert.NeedsPower".Translate());
            }
            else if (mc != null && now < mc.NextAllowedTick)
            {
                int rem = mc.NextAllowedTick - now;
                cmdRed.Disable("ST.RedAlert.Recharging".Translate(rem.ToStringTicksToPeriod()));
            }
            yield return cmdRed;
        }

        void TriggerYellow()
        {
            var map = parent.Map; if (map == null) return;

            var power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                Messages.Message("ST.YellowAlert.NeedsPower".Translate(), parent, MessageTypeDefOf.RejectInput); 
                return;
            }

            var mc = map.GetComponent<MapComponent_AlertPanel>();
            int now = Find.TickManager.TicksGame;
            if (mc != null && now < mc.NextAllowedTickYellow)
            {
                int rem = mc.NextAllowedTickYellow - now;
                Messages.Message("ST.YellowAlert.Recharging".Translate(rem.ToStringTicksToPeriod()), parent, MessageTypeDefOf.RejectInput);
                return;
            }

            // Hediff anwenden
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ST_YellowAlert");
            if (hediffDef == null)
            {
                Messages.Message("Missing hediff: ST_YellowAlert", parent, MessageTypeDefOf.RejectInput);
                return;
            }
            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                var existing = p.health?.hediffSet?.GetFirstHediffOfDef(hediffDef);
                HediffWithComps h = existing as HediffWithComps ?? (p.health?.AddHediff(hediffDef) as HediffWithComps);
                var disp = h?.TryGetComp<HediffComp_Disappears>();
                if (disp != null) disp.ticksToDisappear = Props.yellowDurationTicks;

                MoteMaker.ThrowText(p.DrawPos, map, "YELLOW ALERT", new Color(1f, 0.95f, 0.2f), 1.2f);
            }

            // Blink (gelb) + kurzer Ping
            SpawnBlink(isRed:false, scale:1.1f);
            var ping = DefDatabase<SoundDef>.GetNamedSilentFail("ST_YellowAlert_Ping");
            ping?.PlayOneShot(SoundInfo.OnCamera());

            // Cooldown/Blinkfenster setzen
            if (mc != null)
            {
                mc.BlinkUntilTickYellow = now + 300;              // ~5s
                mc.NextAllowedTickYellow = now + Props.yellowCooldownTicks;
            }

            // Panel gelb einfärben
            SetPanelTint(new Color(1f, 0.92f, 0.2f));

            Messages.Message("ST.YellowAlert.Engaged".Translate(), parent, MessageTypeDefOf.PositiveEvent);
        }

        void TriggerRed()
        {
            var map = parent.Map; if (map == null) return;

            var power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                Messages.Message("ST.RedAlert.NeedsPower".Translate(), parent, MessageTypeDefOf.RejectInput);
                return;
            }

            var mc = map.GetComponent<MapComponent_AlertPanel>();
            int now = Find.TickManager.TicksGame;
            if (mc != null && now < mc.NextAllowedTick)
            {
                int rem = mc.NextAllowedTick - now;
                Messages.Message("ST.RedAlert.Recharging".Translate(rem.ToStringTicksToPeriod()), parent, MessageTypeDefOf.RejectInput);
                return;
            }

            // Hediff anwenden
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ST_RedAlert");
            if (hediffDef == null)
            {
                Messages.Message("ST.RedAlert.MissingHediff".Translate(), parent, MessageTypeDefOf.RejectInput);
                return;
            }
            foreach (var p in map.mapPawns.FreeColonistsSpawned)
            {
                var existing = p.health?.hediffSet?.GetFirstHediffOfDef(hediffDef);
                HediffWithComps h = existing as HediffWithComps ?? (p.health?.AddHediff(hediffDef) as HediffWithComps);
                var disp = h?.TryGetComp<HediffComp_Disappears>();
                if (disp != null) disp.ticksToDisappear = Props.redDurationTicks;

                MoteMaker.ThrowText(p.DrawPos, map, "RED ALERT", Color.red, 1.4f);
            }

            // Blink (rot) + Sounds
            SpawnBlink(isRed:true, scale:1.2f);

            // kurzer Ping
            var ping = DefDatabase<SoundDef>.GetNamedSilentFail("ST_RedAlert_SirenPing");
            ping?.PlayOneShot(SoundInfo.OnCamera());

            // loopende Sirene (optional)
            var mc2 = mc;
            if (Props.playRedSirenLoop && mc2 != null)
            {
                var longDef = DefDatabase<SoundDef>.GetNamedSilentFail("ST_RedAlert_SirenLong");
                if (longDef != null)
                {
                    mc2.activeSiren?.End();
                    mc2.activeSiren = SoundStarter.TrySpawnSustainer(longDef, SoundInfo.OnCamera());
                    mc2.activeSiren?.Maintain();
                }
            }

            // Cooldown/Blinkfenster setzen
            if (mc != null)
            {
                mc.BlinkUntilTick = now + 360;                    // ~6s
                mc.NextAllowedTick = now + Props.redCooldownTicks;
            }

            // Panel rot einfärben
            SetPanelTint(new Color(0.95f, 0.2f, 0.2f));

            Messages.Message("ST.RedAlert.Engaged".Translate(), parent, MessageTypeDefOf.PositiveEvent);
        }

        // --- Helpers ---

        void SpawnBlink(bool isRed, float scale)
        {
            // Zwei farbige Flecks aus XML (API erlaubt kein Per-Instance-Tint)
            string defName = isRed ? "ST_AlertBlink_Red" : "ST_AlertBlink_Yellow";
            var def = DefDatabase<FleckDef>.GetNamedSilentFail(defName) ?? FleckDefOf.Smoke;

            FleckMaker.AttachedOverlay(parent, def, Vector3.zero, scale);
        }

        void SetPanelTint(Color c)
        {
            var cc = parent.TryGetComp<CompColorable>();
            if (cc != null)
            {
                cc.SetColor(c);
                parent.Notify_ColorChanged();
            }
        }
    }
}
