using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;  
using UnityEngine;
using Verse;

namespace YASTM
{
    public class CompProperties_StarfleetAid : CompProperties
    {
        public string useLabel = "Request Starfleet Aid";
        public float cooldownDays = 10f;
        public int silverCost = 200;
        public int goodwillCost = 6;
        public int minGoodwill = 40;
        public string federationFactionDef = "Federation_Of_Planets";
        public int useDuration = 120; // optional: "cast time" flavor

        public CompProperties_StarfleetAid()
        {
            this.compClass = typeof(CompStarfleetAid);
        }
    }

    public class CompStarfleetAid : ThingComp
    {
        public CompProperties_StarfleetAid Props => (CompProperties_StarfleetAid)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;

            var power = parent.GetComp<CompPowerTrader>();
            bool hasPower = power == null || power.PowerOn;

            Command_Action cmd = new Command_Action
            {
                defaultLabel = Props.useLabel,
                defaultDesc  = "Request a limited Starfleet resupply drop (global cooldown).",
                icon         = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", false),
                action       = TryRequestAid
            };

            var wc = Find.World.GetComponent<WorldComponent_StarfleetAid>();
            int ticksNow = Find.TickManager.TicksGame;
            int ticksRemaining = Mathf.Max(0, wc.NextAllowedTick - ticksNow);

            // Disable reasons
            if (!hasPower)
            {
                cmd.Disable("Requires power.");
            }
            else if (ticksRemaining > 0)
            {
                cmd.Disable($"Uplink recharging: {ticksRemaining.ToStringTicksToPeriod()}");
            }
            else
            {
                // goodwill gate (soft requirement)
                var fed = FindFederationFaction();
                if (fed != null && Faction.OfPlayer != null)
                {
                    int gw = fed.GoodwillWith(Faction.OfPlayer);  // statt fed.PlayerGoodwill
                    if (gw < Props.minGoodwill)
                        cmd.Disable($"Requires goodwill ≥ {Props.minGoodwill} with {fed.Name}. (Current: {gw})");
                }
            }

            yield return cmd;
        }

        void TryRequestAid()
        {
            Map map = parent.Map;
            if (map == null) return;

            var power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                Messages.Message("ComBeacon needs power.", parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            var wc = Find.World.GetComponent<WorldComponent_StarfleetAid>();
            int now = Find.TickManager.TicksGame;
            if (now < wc.NextAllowedTick)
            {
                int rem = wc.NextAllowedTick - now;
                Messages.Message("Uplink recharging: " + rem.ToStringTicksToPeriod(), parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Optional "cast time"
            if (Props.useDuration > 0)
            {
                // simple visual: letter/message; real cast would require a job driver
                MoteMaker.ThrowText(parent.DrawPos, map, "Transmitting...", Color.cyan);
            }

            // Costs
            if (Props.silverCost > 0)
            {
                if (!TryConsumeSilver(map, Props.silverCost))
                {
                    Messages.Message($"Not enough silver nearby ({Props.silverCost} required).", parent, MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            // Goodwill cost (applied after success)
            Faction federation = FindFederationFaction();

            // Fire vanilla ResourcePodCrash (moderate RNG supplies)
            IncidentParms parms = new IncidentParms
            {
                target = map,
                forced = true,
                points = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(map), 120f, 500f) // mild scale (not really used by ResourcePodCrash, but harmless)
            };

            IncidentDef aidIncident = DefDatabase<IncidentDef>.GetNamed("ResourcePodCrash", false);
                if (aidIncident == null)
                {
                    Messages.Message("Aid incident not found: ResourcePodCrash", parent, MessageTypeDefOf.RejectInput);
                    return;
                }
            bool ok = aidIncident.Worker.TryExecute(parms);
                
            if (ok)
            {
                // set global cooldown
                int cdTicks = Mathf.RoundToInt(Props.cooldownDays * 60000f);
                wc.NextAllowedTick = now + cdTicks;

                if (federation != null && Props.goodwillCost > 0)
                {
                    federation.TryAffectGoodwillWith(
                        Faction.OfPlayer,
                        -Props.goodwillCost,
                        canSendMessage: true
                    );
                }

                // Feedback
                Messages.Message("Starfleet aid supplies inbound.", parent, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Aid request failed.", parent, MessageTypeDefOf.RejectInput);
            }
        }

        bool TryConsumeSilver(Map map, int amount)
        {
            int remaining = amount;
            // simple scan over all silver stacks on map (owned area would be better, but keep it simple)
            List<Thing> stacks = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            // sort by distance to beacon
            stacks.Sort((a, b) => (a.Position - parent.Position).LengthHorizontalSquared.CompareTo((b.Position - parent.Position).LengthHorizontalSquared));

            foreach (var t in stacks)
            {
                if (!t.Spawned) continue;
                int take = Mathf.Min(remaining, t.stackCount);
                if (take <= 0) break;

                Thing taken = t.SplitOff(take);
                remaining -= take;
                taken.Destroy(DestroyMode.Vanish);

                if (remaining <= 0) break;
            }

            return remaining <= 0;
        }

        Faction FindFederationFaction()
        {
            if (Props.federationFactionDef.NullOrEmpty()) return null;
            var def = DefDatabase<FactionDef>.GetNamedSilentFail(Props.federationFactionDef);
            if (def == null) return null;
            return Find.FactionManager.FirstFactionOfDef(def);
        }
    }
}
