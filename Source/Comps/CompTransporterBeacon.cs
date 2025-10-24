using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace YASTM
{
    public class CompProperties_TransporterBeacon : CompProperties
    {
        public int cooldownBeamInTicks = 300000;   // ~5 Tage
        public int cooldownBeamOutTicks = 120000;  // ~2 Tage
        public float maxBeamOutDistance = 50f;     // Max. Zielentfernung für Beam-out

        public float operatorMaxDistance = 6f;     // Offizier muss so nah am Beacon stehen
        public List<string> requiredRankTraitsBeamIn;   // z.B. LtJG+
        public List<string> requiredRankTraitsBeamOut;  // z.B. Commander+

        public string soundDefName = "ST_Transporter_Beam";  

        public CompProperties_TransporterBeacon()
        {
            compClass = typeof(CompTransporterBeacon);
        }
    }

    public class CompTransporterBeacon : ThingComp
    {
        public CompProperties_TransporterBeacon Props => (CompProperties_TransporterBeacon)props;

        private int nextAllowedBeamInTick;
        private int nextAllowedBeamOutTick;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAllowedBeamInTick, "ST_nextBeamIn", 0);
            Scribe_Values.Look(ref nextAllowedBeamOutTick, "ST_nextBeamOut", 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction != Faction.OfPlayer) yield break;
            var map = parent.Map;
            var power = parent.TryGetComp<CompPowerTrader>();
            int now = Find.TickManager.TicksGame;

            // ---- Beam-in ----
            var beamIn = new Command_Action
            {
                defaultLabel = "ST.Transporter.BeamIn".Translate(),
                defaultDesc  = "ST.Transporter.BeamIn.Desc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/BeamSupply", false),
                action       = () =>
                {
                    if (!TryFindEligibleOperator(Props.requiredRankTraitsBeamIn, out var op, out var reason))
                    {
                        Messages.Message(reason ?? "ST.Transporter.RequiresOfficerGeneric".Translate(),
                            parent, MessageTypeDefOf.RejectInput);
                        return;
                    }
                    DoBeamIn(op);
                }
            };

            if (power != null && !power.PowerOn)
                beamIn.Disable("ST.Common.RequiresPower".Translate());
            else if (now < nextAllowedBeamInTick)
                beamIn.Disable("ST.Common.Recharging".Translate((nextAllowedBeamInTick - now).ToStringTicksToPeriod()));
            else if (!HasEligibleOperator(Props.requiredRankTraitsBeamIn, out _))
                beamIn.Disable(NeedOfficerReason(Props.requiredRankTraitsBeamIn));

            yield return beamIn;

            // ---- Beam-out ----
            var beamOut = new Command_Target
            {
                defaultLabel = "ST.Transporter.BeamOut".Translate(),
                defaultDesc  = "ST.Transporter.BeamOut.Desc".Translate(),
                icon         = ContentFinder<Texture2D>.Get("UI/Icons/Gizmos/BeamEmerg", false),
                targetingParams = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetLocations = false,
                    canTargetSelf = false,
                    validator = t =>
                    {
                        var p = t.Thing as Pawn;
                        if (p == null || p.Map != map) return false;
                        return p.Faction == Faction.OfPlayer;
                    }
                },
                action = target =>
                {
                    var pawn = target.Thing as Pawn;
                    if (pawn == null) return;

                    if (!TryFindEligibleOperator(Props.requiredRankTraitsBeamOut, out var op, out var reason))
                    {
                        Messages.Message(reason ?? "ST.Transporter.RequiresOfficerGeneric".Translate(),
                            parent, MessageTypeDefOf.RejectInput);
                        return;
                    }
                    TryBeamOutPawn(pawn, op);
                }
            };

            if (power != null && !power.PowerOn)
                beamOut.Disable("ST.Common.RequiresPower".Translate());
            else if (now < nextAllowedBeamOutTick)
                beamOut.Disable("ST.Common.Recharging".Translate((nextAllowedBeamOutTick - now).ToStringTicksToPeriod()));
            else if (!HasEligibleOperator(Props.requiredRankTraitsBeamOut, out _))
                beamOut.Disable(NeedOfficerReason(Props.requiredRankTraitsBeamOut));

            yield return beamOut;
        }

        // ---------- Operator-Findung / Rang-Gate ----------
        private bool HasEligibleOperator(List<string> requiredTraits, out Pawn op)
        {
            return TryFindEligibleOperator(requiredTraits, out op, out _);
        }

        private bool TryFindEligibleOperator(List<string> requiredTraits, out Pawn op, out string failReason)
        {
            op = null;
            failReason = null;

            var map = parent.Map;
            if (map == null) { failReason = "ST.Transporter.RequiresOfficerGeneric".Translate(); return false; }

            var pawns = map.mapPawns.FreeColonistsSpawned;
            Pawn best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < pawns.Count; i++)
            {
                var p = pawns[i];
                if (p.Downed || p.InMentalState || p.IsPrisoner) continue;
                float dist = p.Position.DistanceTo(parent.Position);
                if (dist > Props.operatorMaxDistance) continue;
                if (!PawnHasAnyRank(p, requiredTraits)) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = p;
                }
            }

            if (best == null)
            {
                failReason = NeedOfficerReason(requiredTraits);
                return false;
            }

            op = best;
            return true;
        }

        private static bool PawnHasAnyRank(Pawn p, List<string> rankDefNames)
        {
            if (rankDefNames == null || rankDefNames.Count == 0) return true; 
            var traits = p.story?.traits;
            if (traits == null) return false;
            for (int i = 0; i < rankDefNames.Count; i++)
            {
                var tdef = DefDatabase<TraitDef>.GetNamedSilentFail(rankDefNames[i]);
                if (tdef != null && traits.HasTrait(tdef)) return true;
            }
            return false;
        }

        private string NeedOfficerReason(List<string> rankDefNames)
        {
            // Kurzer, generischer Hinweis inkl. Distanz
            if (rankDefNames != null && rankDefNames.Count > 0)
                return "ST.Transporter.RequiresOfficerRanked".Translate(Props.operatorMaxDistance.ToString("0"));
            return "ST.Transporter.RequiresOfficerGeneric".Translate();
        }

        // ---------- Beam-in ----------
        private void DoBeamIn(Pawn operatorPawn)
        {
            var map = parent.Map;
            if (map == null) return;

            int now = Find.TickManager.TicksGame;
            if (now < nextAllowedBeamInTick) return;

            if (!CellFinder.TryFindRandomCellNear(parent.Position, map, 3,
                    c => c.Standable(map) && c.InBounds(map) && !c.Fogged(map), out var cell))
                cell = parent.Position;

            var things = MakeLogisticsPackage();
            foreach (var t in things)
                GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);

            PlayTransporterFX(cell, map);
            Messages.Message("ST.Transporter.BeamIn.Done".Translate(), new LookTargets(cell, map), MessageTypeDefOf.PositiveEvent);

            nextAllowedBeamInTick = now + Props.cooldownBeamInTicks;
        }

        private List<Thing> MakeLogisticsPackage()
        {
            var list = new List<Thing>();
            void Add(string defName, int count)
            {
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null || count <= 0) return;
                int remaining = count;
                while (remaining > 0)
                {
                    var t = ThingMaker.MakeThing(def);
                    t.stackCount = Mathf.Min(remaining, def.stackLimit);
                    list.Add(t);
                    remaining -= t.stackCount;
                }
            }

            Add("MedicineIndustrial", 6);
            Add("PackageSurvivalMeal", 12);
            Add("ComponentIndustrial", 4);
            Add("Steel", 80);

            return list;
        }

        // ---------- Beam-out ----------
        private void TryBeamOutPawn(Pawn pawn, Pawn operatorPawn)
        {
            var map = parent.Map;
            if (pawn == null || map == null) return;

            int now = Find.TickManager.TicksGame;
            if (now < nextAllowedBeamOutTick) return;

            if (pawn.Position.DistanceTo(parent.Position) > Props.maxBeamOutDistance)
            {
                Messages.Message("ST.Transporter.BeamOut.TooFar".Translate(), pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            IntVec3 dest = parent.Position;
            if (!dest.Walkable(map))
                dest = CellFinder.StandableCellNear(parent.Position, map, 1);

            PlayTransporterFX(pawn.Position, map);
            if (pawn.Spawned) pawn.DeSpawn();
            GenSpawn.Spawn(pawn, dest, map);
            PlayTransporterFX(dest, map);

            var nausea = DefDatabase<HediffDef>.GetNamedSilentFail("ST_TransporterNausea");
            if (nausea != null)
            {
                var h = pawn.health.AddHediff(nausea);
                var disp = h.TryGetComp<HediffComp_Disappears>();
                if (disp != null) disp.ticksToDisappear = Rand.RangeInclusive(15000, 25000);
            }

            Messages.Message("ST.Transporter.BeamOut.Done".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            nextAllowedBeamOutTick = now + Props.cooldownBeamOutTicks;
        }

        private void PlayTransporterFX(IntVec3 cell, Map map)
        {
            if (!Props.soundDefName.NullOrEmpty())
            {
                var sd = DefDatabase<SoundDef>.GetNamedSilentFail(Props.soundDefName);
                if (sd != null) SoundStarter.PlayOneShot(sd, new TargetInfo(cell, map));
            }
            FleckMaker.Static(cell.ToVector3Shifted(), map, FleckDefOf.MicroSparks, 1.2f);
            FleckMaker.Static(cell.ToVector3Shifted(), map, FleckDefOf.AirPuff, 1.0f);
        }
    }
}
