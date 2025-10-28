// Source/Comps/CompTransporterBeacon.cs
using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    public class CompProperties_TransporterBeacon : CompProperties
    {
        public int energyCostPerUse = 1200;
        public int warmupTicks = 60;
        public int cooldownTicks = 600;
        public int maxLinkDistance = 60;
        public List<string> requiredRankTraitsBeamOut;
        public List<string> requiredRankTraitsBeamIn;

        public CompProperties_TransporterBeacon()
        {
            compClass = typeof(CompTransporterBeacon);
        }
    }

    public class CompTransporterBeacon : ThingComp
    {
        public CompProperties_TransporterBeacon Props => (CompProperties_TransporterBeacon)props;

        private static readonly Dictionary<Map, List<CompTransporterBeacon>> Registry = new();

        private int nextUsableTick;
        private int _nextLightCacheTick;
        private bool _isPoweredCached;
        private int _nextPruneTick;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref nextUsableTick, "ST_nextUsableTick", 0);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var map = parent.Map;
            if (map != null)
            {
                if (!Registry.TryGetValue(map, out var list))
                {
                    list = new List<CompTransporterBeacon>();
                    Registry[map] = list;
                }
                if (!list.Contains(this)) list.Add(this);
            }
        }

        private static void PruneRegistry(Map map)
        {
            if (map == null) return;
            if (!Registry.TryGetValue(map, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var c = list[i];
                if (c == null || c.parent == null || c.parent.Destroyed)
                    list.RemoveAt(i);
            }
        }

        private bool HasPowerLightCached()
        {
            if (Find.TickManager.TicksGame >= _nextLightCacheTick)
            {
                _nextLightCacheTick = Find.TickManager.TicksGame + 60;
                var p = parent.TryGetComp<CompPowerTrader>();
                var f = parent.TryGetComp<CompFlickable>();
                _isPoweredCached = (p == null || p.PowerOn) && (f == null || f.SwitchIsOn);
            }
            return _isPoweredCached;
        }

        private int EffectiveMaxLink() => Props.maxLinkDistance;

        private static bool PawnHasAnyRank(Pawn pawn, List<string> traitDefNames)
        {
            if (pawn?.story?.traits == null || traitDefNames == null || traitDefNames.Count == 0) return true;
            foreach (var defName in traitDefNames)
            {
                var td = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
                if (td != null && pawn.story.traits.HasTrait(td))
                    return true;
            }
            return false;
        }

        private IEnumerable<CompTransporterBeacon> TargetBeaconsInRange()
        {
            var map = parent.Map;
            if (map == null) yield break;

            if (Find.TickManager.TicksGame >= _nextPruneTick)
            {
                _nextPruneTick = Find.TickManager.TicksGame + 600;
                PruneRegistry(map);
            }

            if (!Registry.TryGetValue(map, out var list)) yield break;

            int maxDist = EffectiveMaxLink();
            float maxDistSq = maxDist * maxDist;

            foreach (var b in list)
            {
                if (b == null || b.parent == null || b.parent.Destroyed) continue;
                if (b == this) continue;
                if ((b.parent.Position - parent.Position).LengthHorizontalSquared <= maxDistSq)
                    yield return b;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // ---- Beam OUT ----
            var cmdOut = new Command_Action
            {
                defaultLabel = "ST.Transporter.BeamOut".Translate(),
                defaultDesc  = "ST.Transporter.BeamOut.Desc".Translate(EffectiveMaxLink()),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ST_BeamOut", false),
                action       = BeginBeamOutTargeting
            };

            bool disableOut = !HasPowerLightCached() || Find.TickManager.TicksGame < nextUsableTick;
            if (disableOut)
            {
                string reason = !HasPowerLightCached()
                    ? "ST.Common.NeedsPower".Translate()
                    : "ST.Common.Recharging".Translate((nextUsableTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());
                cmdOut.Disable(reason);
            }
            yield return cmdOut;

            // ---- Beam IN ----
            var cmdIn = new Command_Action
            {
                defaultLabel = "ST.Transporter.BeamIn".Translate(),
                defaultDesc  = "ST.Transporter.BeamIn.Desc".Translate(EffectiveMaxLink()),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ST_BeamIn", false),
                action       = BeginBeamInTargeting
            };

            bool disableIn = !HasPowerLightCached() || Find.TickManager.TicksGame < nextUsableTick;
            if (disableIn)
            {
                string reason = !HasPowerLightCached()
                    ? "ST.Common.NeedsPower".Translate()
                    : "ST.Common.Recharging".Translate((nextUsableTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());
                cmdIn.Disable(reason);
            }
            yield return cmdIn;
        }

        private void BeginBeamOutTargeting()
        {
            var tp = new TargetingParameters { canTargetPawns = true, canTargetAnimals = false, canTargetBuildings = false, canTargetSelf = false };
            Find.Targeter.BeginTargeting(tp, target =>
            {
                var pawn = target.Thing as Pawn;
                if (pawn == null || pawn.Dead || pawn.Map != parent.Map) return;
                if (!PawnHasAnyRank(pawn, Props.requiredRankTraitsBeamOut))
                {
                    Messages.Message("ST.Transporter.RankTooLow".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.RejectInput);
                    return;
                }

                var dest = ClosestTargetBeacon();
                if (dest == null)
                {
                    Messages.Message("ST.Transporter.NoTargetBeacon".Translate(EffectiveMaxLink()), MessageTypeDefOf.RejectInput);
                    return;
                }

                TryBeam(pawn, dest.parent.Position, dest.parent.Map);
            });
        }

        private void BeginBeamInTargeting()
        {
            var tp = new TargetingParameters { canTargetPawns = true, canTargetAnimals = false, canTargetBuildings = false, canTargetSelf = false };
            Find.Targeter.BeginTargeting(tp, target =>
            {
                var pawn = target.Thing as Pawn;
                if (pawn == null || pawn.Dead) return;

                if (!PawnHasAnyRank(pawn, Props.requiredRankTraitsBeamIn))
                {
                    Messages.Message("ST.Transporter.RankTooLow".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.RejectInput);
                    return;
                }

                if (pawn.Map != parent.Map)
                {
                    Messages.Message("ST.Transporter.SameMapOnly".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }
                if ((pawn.Position - parent.Position).LengthHorizontal > EffectiveMaxLink())
                {
                    Messages.Message("ST.Transporter.TooFar".Translate(EffectiveMaxLink()), MessageTypeDefOf.RejectInput);
                    return;
                }

                TryBeam(pawn, parent.Position, parent.Map);
            });
        }

        private CompTransporterBeacon ClosestTargetBeacon()
        {
            CompTransporterBeacon best = null;
            float bestDistSq = float.MaxValue;
            foreach (var b in TargetBeaconsInRange())
            {
                float d = (b.parent.Position - parent.Position).LengthHorizontalSquared;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = b;
                }
            }
            return best;
        }

        private void TryBeam(Pawn pawn, IntVec3 dest, Map map)
        {
            if (!HasPowerLightCached())
            {
                Messages.Message("ST.Common.NeedsPower".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            if (Find.TickManager.TicksGame < nextUsableTick)
            {
                Messages.Message("ST.Common.Recharging".Translate(
                    (nextUsableTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()), MessageTypeDefOf.RejectInput);
                return;
            }

            var power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                Messages.Message("ST.Common.NeedsPower".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            int warmup = Math.Max(0, Props.warmupTicks);
            if (warmup > 0)
                MoteMaker.ThrowText(parent.TrueCenter(), parent.Map, "ST.Transporter.Warmup".Translate(), 2f);

            TransporterVFX.PlayBeam(parent.Map, parent.Position);

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (pawn == null || pawn.Destroyed || map == null) return;
                if (pawn.Map != map)
                {
                    Messages.Message("ST.Transporter.SameMapOnly".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                TransporterVFX.PlayBeam(map, dest);

                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, CellFinder.StandableCellNear(dest, map, 2), map);

                nextUsableTick = Find.TickManager.TicksGame + Math.Max(Props.cooldownTicks, 60);
            });
        }
    }
}
