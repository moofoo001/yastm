using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YASTM
{
    // -------------------- PROPS --------------------

    public class CompProperties_TransporterConsole : CompProperties
    {
        public int energyCostPerUse = 1200;
        public int warmupTicks = 60;
        public int cooldownTicks = 600;
        public int maxLinkDistance = 60;

        public List<string> requiredRankTraitsBeamOut;
        public List<string> requiredRankTraitsBeamIn;

        public CompProperties_TransporterConsole()
        {
            compClass = typeof(CompTransporterConsole);
        }
    }

    /// <summary>
    /// XML-Alias für alte Einträge (Class="YASTM.CompProperties_TransporterBeacon").
    /// Nutzt intern dieselbe Comp.
    /// </summary>
    public class CompProperties_TransporterBeacon : CompProperties_TransporterConsole
    {
        public CompProperties_TransporterBeacon()
        {
            compClass = typeof(CompTransporterConsole);
        }
    }

    // -------------------- COMP --------------------

    public class CompTransporterConsole : ThingComp
    {
        private static readonly Dictionary<Map, List<CompTransporterConsole>> Registry = new();

        private int nextUsableTick;
        private int _nextLightCacheTick;
        private bool _isPoweredCached;
        private int _nextPruneTick;

        public CompProperties_TransporterConsole Props => (CompProperties_TransporterConsole)props;

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
                    list = new List<CompTransporterConsole>();
                    Registry[map] = list;
                }
                if (!list.Contains(this)) list.Add(this);
            }
        }

        // 1.6: PostDeSpawn(Map) gibt es nicht mehr -> PostDestroy verwenden
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (previousMap != null && Registry.TryGetValue(previousMap, out var list))
            {
                list.Remove(this);
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

        private bool HasPowerCached()
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

        private IEnumerable<CompTransporterConsole> TargetConsolesInRange()
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

            foreach (var c in list)
            {
                if (c == this || c == null || c.parent == null || c.parent.Destroyed) continue;
                if ((c.parent.Position - parent.Position).LengthHorizontalSquared <= maxDistSq)
                    yield return c;
            }
        }

        private CompTransporterConsole ClosestTargetConsoleWithPad()
        {
            CompTransporterConsole best = null;
            float bestDistSq = float.MaxValue;

            foreach (var c in TargetConsolesInRange())
            {
                if (!TransporterPadUtil.TryGetLinkedPad((Building)c.parent, out var pad)) continue;
                if (!TransporterPadUtil.IsPoweredOn(c.parent)) continue;
                if (!TransporterPadUtil.IsPoweredOn(pad)) continue;

                float d = (c.parent.Position - parent.Position).LengthHorizontalSquared;
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    best = c;
                }
            }
            return best;
        }

        // -------------------- GIZMOS --------------------

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Beam OUT
            var cmdOut = new Command_Action
            {
                defaultLabel = "ST.Transporter.BeamOut".Translate(),
                defaultDesc  = "ST.Transporter.BeamOut.Desc".Translate(EffectiveMaxLink()),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ST_BeamOut", false),
                action       = BeginBeamOutTargeting
            };
            if (!CanUseNow(out string reasonOut)) cmdOut.Disable(reasonOut);
            yield return cmdOut;

            // Beam IN
            var cmdIn = new Command_Action
            {
                defaultLabel = "ST.Transporter.BeamIn".Translate(),
                defaultDesc  = "ST.Transporter.BeamIn.Desc".Translate(EffectiveMaxLink()),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/ST_BeamIn", false),
                action       = BeginBeamInTargeting
            };
            if (!CanUseNow(out string reasonIn)) cmdIn.Disable(reasonIn);
            yield return cmdIn;
        }

        private bool CanUseNow(out string reason)
        {
            reason = null;
            if (!HasPowerCached())
            {
                reason = "ST.Common.NeedsPower".Translate();
                return false;
            }
            if (Find.TickManager.TicksGame < nextUsableTick)
            {
                reason = "ST.Common.Recharging".Translate(
                    (nextUsableTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());
                return false;
            }
            if (!TransporterPadUtil.TryGetLinkedPad((Building)parent, out var localPad))
            {
                reason = "No linked transporter pad online.";
                return false;
            }
            if (!TransporterPadUtil.IsPoweredOn(localPad))
            {
                reason = "Linked pad has no power.";
                return false;
            }
            return true;
        }

        // -------------------- TARGETING --------------------

        private void BeginBeamOutTargeting()
        {
            var tp = new TargetingParameters
            {
                canTargetPawns = true, canTargetAnimals = false, canTargetBuildings = false, canTargetSelf = false
            };

            Find.Targeter.BeginTargeting(tp, target =>
            {
                var pawn = target.Thing as Pawn;
                if (pawn == null || pawn.Dead || pawn.Map != parent.Map) return;

                if (!PawnHasAnyRank(pawn, Props.requiredRankTraitsBeamOut))
                {
                    Messages.Message("ST.Transporter.RankTooLow".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.RejectInput);
                    return;
                }
                if (!TransporterPadUtil.TryGetLinkedPad((Building)parent, out var localPad))
                {
                    Messages.Message("No linked transporter pad online.", parent, MessageTypeDefOf.RejectInput);
                    return;
                }

                var destConsole = ClosestTargetConsoleWithPad();
                if (destConsole == null)
                {
                    Messages.Message("ST.Transporter.NoTargetBeacon".Translate(EffectiveMaxLink()), MessageTypeDefOf.RejectInput);
                    return;
                }
                if (!TransporterPadUtil.TryGetLinkedPad((Building)destConsole.parent, out var destPad))
                {
                    Messages.Message("Destination console has no linked pad online.", destConsole.parent, MessageTypeDefOf.RejectInput);
                    return;
                }

                var fromCell = TransporterPadUtil.GetPadCell(localPad);
                var toCell   = TransporterPadUtil.GetPadCell(destPad);

                TryBeam(pawn, fromCell, toCell, parent.Map);
            });
        }

        private void BeginBeamInTargeting()
        {
            var tp = new TargetingParameters
            {
                canTargetPawns = true, canTargetAnimals = false, canTargetBuildings = false, canTargetSelf = false
            };

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
                if (!TransporterPadUtil.TryGetLinkedPad((Building)parent, out var localPad))
                {
                    Messages.Message("No linked transporter pad online.", parent, MessageTypeDefOf.RejectInput);
                    return;
                }

                var fromCell = pawn.Position;
                var toCell   = TransporterPadUtil.GetPadCell(localPad);

                TryBeam(pawn, fromCell, toCell, parent.Map);
            });
        }

        // -------------------- CORE --------------------

        private void TryBeam(Pawn pawn, IntVec3 fromCell, IntVec3 toCell, Map map)
        {
            if (!HasPowerCached())
            {
                Messages.Message("ST.Common.NeedsPower".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            if (Find.TickManager.TicksGame < nextUsableTick)
            {
                Messages.Message("ST.Common.Recharging".Translate(
                    (nextUsableTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()),
                    MessageTypeDefOf.RejectInput);
                return;
            }
            if (!fromCell.IsValid || !toCell.IsValid)
            {
                Messages.Message("Invalid pad cell.", MessageTypeDefOf.RejectInput);
                return;
            }

            // Warmup (opt.)
            int warmup = Math.Max(0, Props.warmupTicks);
            if (warmup > 0)
                MoteMaker.ThrowText(parent.TrueCenter(), parent.Map, "ST.Transporter.Warmup".Translate(), 2f);

            // Start-VFX
            TransporterVFX.PlayBeam(map, fromCell);

            // Teleport
            pawn.DeSpawn();
            var safeTo = CellFinder.StandableCellNear(toCell, map, 1);
            GenSpawn.Spawn(pawn, safeTo, map);

            // Ziel-VFX + 3s Rematerialisierung
            TransporterVFX.PlayBeam(map, toCell);
            TransporterVFX.BeginRematerialize(pawn, 180);

            // Cooldown
            nextUsableTick = Find.TickManager.TicksGame + Math.Max(Props.cooldownTicks, 60);
        }
    }
}
