using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;      // für WorldComponent
using UnityEngine;
using Verse;
using System.Linq;

namespace YASTM
{
    public class CompProperties_StarfleetAid : CompProperties
    {
        public string useLabel = "Request Starfleet Aid";
        public float  cooldownDays = 10f;
        public int    silverCost   = 200;
        public int    goodwillCost = 6;
        public int    minGoodwill  = 40;
        public string federationFactionDef = "Federation_Of_Planets";
        public int    useDuration  = 120;

        public CompProperties_StarfleetAid()
        {
            this.compClass = typeof(CompStarfleetAid);
        }
    }

    public class CompStarfleetAid : ThingComp
    {
        public CompProperties_StarfleetAid Props => (CompProperties_StarfleetAid)props;


        float CooldownDays => (YASTM_Mod.Settings?.AidCooldownDays  ?? Props.cooldownDays);
        int   SilverCost   => (YASTM_Mod.Settings?.AidSilverCost    ?? Props.silverCost);
        int   GoodwillCost => (YASTM_Mod.Settings?.AidGoodwillCost  ?? Props.goodwillCost);
        int   MinGoodwill  => (YASTM_Mod.Settings?.AidMinGoodwill   ?? Props.minGoodwill);
        bool HasCommanderOrCaptain(Map map)
        {
            if (map == null) return false;

            var defCmd = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Rank_Commander");
            var defCpt = DefDatabase<TraitDef>.GetNamedSilentFail("ST_Rank_Captain");

            bool HasRankTrait(Pawn p) =>
                p?.story?.traits != null &&
                ((defCmd != null && p.story.traits.HasTrait(defCmd)) ||
                (defCpt != null && p.story.traits.HasTrait(defCpt)));

            bool HasRankPip(Pawn p)
            {
                var wa = p?.apparel?.WornApparel;
                if (wa == null) return false;
                return wa.Any(a =>
                {
                    var dn = a.def?.defName;
                    return dn == "ST_RankPips_Commander" || dn == "ST_RankPips_Captain";
                });
            }

            foreach (var p in map.mapPawns.FreeColonistsSpawned)
                if (HasRankTrait(p) || HasRankPip(p)) return true;

            return false;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer)
                yield break;

            var power = parent.GetComp<CompPowerTrader>();
            bool hasPower = power == null || power.PowerOn;

            var cmd = new Command_Action
            {
                defaultLabel = Props.useLabel ?? "Request Starfleet Aid",
                defaultDesc  = "Request a limited Starfleet resupply drop (global cooldown).",
                icon         = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", false),
                action       = TryRequestAid
            };

            var wc = Find.World.GetComponent<WorldComponent_StarfleetAid>();
            int ticksNow = Find.TickManager.TicksGame;
            int nextAllowed = wc?.NextAllowedTick ?? 0;
            int ticksRemaining = Mathf.Max(0, nextAllowed - ticksNow);

            if (!hasPower)
            {
                cmd.Disable("Requires power.");
            }
            else if (ticksRemaining > 0)
            {
                cmd.Disable($"Uplink recharging: {ticksRemaining.ToStringTicksToPeriod()}");
            }
            else if (!HasCommanderOrCaptain(parent.Map))
            {
                cmd.Disable("Requires a Commander or Captain on this map.");
            }
            else
            {
                // Goodwill-Gate
                var fed = FindFederationFaction();
                if (fed != null && Faction.OfPlayer != null)
                {
                    int gw = fed.GoodwillWith(Faction.OfPlayer);
                    if (gw < MinGoodwill)
                        cmd.Disable($"Requires goodwill ≥ {MinGoodwill} with {fed.Name}. (Current: {gw})");
                }
            }

            yield return cmd;
        }

        void TryRequestAid()
        {
            if (!HasCommanderOrCaptain(parent.Map))
            {
                Messages.Message("Aid request requires a Commander or Captain present.", parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

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
            if (wc != null && now < wc.NextAllowedTick)
            {
                int rem = wc.NextAllowedTick - now;
                Messages.Message("Uplink recharging: " + rem.ToStringTicksToPeriod(), parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (Props.useDuration > 0)
                MoteMaker.ThrowText(parent.DrawPos, map, "Transmitting...", Color.cyan);

            // Kosten: Silber
            if (SilverCost > 0)
            {
                if (!TryConsumeSilver(map, SilverCost))
                {
                    Messages.Message($"Not enough silver nearby ({SilverCost} required).", parent, MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            // Incident auslösen (unser eigener)
            IncidentParms parms = new IncidentParms { target = map, forced = true };
            IncidentDef aidIncident = DefDatabase<IncidentDef>.GetNamed("ST_StarfleetAidDrop", false);
            if (aidIncident == null)
            {
                Messages.Message("Aid incident not found: ST_StarfleetAidDrop", parent, MessageTypeDefOf.RejectInput);
                return;
            }

            bool ok = aidIncident.Worker.TryExecute(parms);
            if (ok)
            {
                // Cooldown setzen
                int cdTicks = Mathf.RoundToInt(CooldownDays * 60000f);
                if (wc != null) wc.NextAllowedTick = now + cdTicks;

                // Goodwill-Kosten (optional)
                var federation = FindFederationFaction();
                if (federation != null && GoodwillCost > 0)
                {
                    federation.TryAffectGoodwillWith(Faction.OfPlayer, -GoodwillCost, canSendMessage: true);
                }

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
            List<Thing> stacks = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            stacks.Sort((a, b) =>
                (a.Position - parent.Position).LengthHorizontalSquared
                .CompareTo((b.Position - parent.Position).LengthHorizontalSquared));

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

